using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Google.Protobuf;
using Grpc.Core;
using LmdbCache;
using LmdbCacheServer.Tables;
using LmdbLight;
using NLog;
using static LmdbCache.KvMetadata.Types;
using static LmdbCache.KvMetadata.Types.Status;
using static LmdbCache.KvMetadata.Types.UpdateAction;
using static LmdbCache.SyncPacket.PacketOneofCase;
using static LmdbCache.SyncPacket.Types;
using static LmdbCache.WriteLogEvent.LoggedEventOneofCase;
using static LmdbCache.WriteLogEvent.Types;

namespace LmdbCacheServer.Replica
{
    public class ReplicatorSource : IDisposable
    {
        private Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private readonly LightningPersistence _lmdb;
        private readonly string _ownReplicaId;
        private readonly KvTable _kvTable;
        private readonly WriteLogTable _wlTable;
        private readonly CancellationToken _cancellationToken;
        private readonly Func<SyncPacket, Task> _responseStreamWriteAsync;
        private readonly Task _pumpTask;

        private readonly ReplicationConfig _replicationConfig;

        public ReplicatorSource(LightningPersistence lmdb, string ownReplicaId,
            KvTable kvTable, WriteLogTable wlTable,
            ReplicationConfig replicationConfig, CancellationToken cancellationToken,
            Func<SyncPacket, Task> responseStreamWriteAsync)
        {
            _lmdb = lmdb;
            _ownReplicaId = ownReplicaId;
            _kvTable = kvTable;
            _wlTable = wlTable;
            _replicationConfig = replicationConfig;
            _cancellationToken = cancellationToken;
            (_pumpTask, _responseStreamWriteAsync) = StartBufferedWrites(responseStreamWriteAsync);
        }

        private (Task, Func<SyncPacket, Task>) StartBufferedWrites(Func<SyncPacket, Task> responseStreamWriteAsync)
        {
            var sinkFlow = new BufferBlock<SyncPacket>();
            var pumpTask = Task.Run(async () =>
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    var msg = await sinkFlow.ReceiveAsync(_cancellationToken); // TODO: Add timeout
                    await responseStreamWriteAsync(msg);
                }
            }, _cancellationToken);

            return (pumpTask, sp => sinkFlow.SendAsync(sp, _cancellationToken));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ByteString GetValue(AbstractTransaction txn, string key) => _kvTable.TryGet(txn, new KvKey(key)).Item2?.Value;

        public void Dispose()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task SyncFrom(ulong since, string[] excludeOriginReplicas)
        {
            var lastPos = since;
            _log.Info($"Received replication request to sync from {lastPos}");
            var until = _lmdb.Read(txn => _wlTable.GetLastClock(txn))?.Item1 ?? 0;

            while (!_cancellationToken.IsCancellationRequested && lastPos <= until)
            {
                var page = _lmdb.Read(txn =>
                {
                    var logPage = _wlTable.GetLogPage(txn, lastPos, _replicationConfig.PageSize)
                        .Where(logItem => logItem.Item1 <= until && !excludeOriginReplicas.Contains(logItem.Item2.OriginatorReplicaId))
                        . // TODO: Add check for IncludeAcked
                        ToArray();

                    foreach (var logItem in logPage)
                    {
                        if (logItem.Item2.LoggedEventCase == WriteLogEvent.LoggedEventOneofCase.Updated && (logItem.Item2.Updated.Value == null || logItem.Item2.Updated.Value.IsEmpty))
                        {
                            var latestValue = GetValue(txn, logItem.Item2.Updated.Key);
                            if (latestValue != null)
                            {
                                logItem.Item2.Updated.Value = latestValue; // TODO: Handle "Value not found"
                            }
                            else
                            {
                                _log.Info($"No value for the key {logItem.Item2.Updated.Key}");
                            }
                        }
                    }

                    return logPage;
                });

                if (page.Length == 0) break;

                {
                    if (_replicationConfig.UseBatching)
                    {
                        lastPos = await ProcessInBatches(page);
                    }
                    else
                    {
                        foreach (var pageItem in page)
                        {
                            var (pos, item) = pageItem;
                            if (!_cancellationToken.IsCancellationRequested)
                            {
                                await _responseStreamWriteAsync(new SyncPacket
                                {
                                    ReplicaId = _ownReplicaId,
                                    Item = new Item { LogEvent = item }
                                });
                                lastPos = pos;
                            }
                        }
                    }

                    lastPos++;
                }
            }

            await _responseStreamWriteAsync(new SyncPacket
            {
                ReplicaId = _ownReplicaId,
                SkipPos = new SkipPos { LastPos = lastPos }
            });

            _log.Info($"Page replicated. Last pos: {lastPos}");

            async Task<ulong> ProcessInBatches((ulong, WriteLogEvent)[] page)
            {
                var batches = page.Aggregate((0, new List<List<Item>>()), (acc, logItem) =>
                    {
                        var (lastBatchBytes, currentBatches) = acc;
                        var itemSize = logItem.Item2.LoggedEventCase == WriteLogEvent.LoggedEventOneofCase.Updated
                            ? logItem.Item2.Updated.Value.Length
                            : 100;
                        if (currentBatches.Count == 0 || lastBatchBytes + itemSize > 3 * 1024 * 1024)
                        {
                            currentBatches.Add(new List<Item>
                                {new Item { LogEvent = logItem.Item2 }});
                            return (itemSize, currentBatches);
                        }

                        currentBatches.Last().Add(new Item {LogEvent = logItem.Item2});
                        return (lastBatchBytes + itemSize, currentBatches);
                    })
                    .Item2;

                foreach (var batch in batches)
                {
                    if (!_cancellationToken.IsCancellationRequested)
                    {
                        //                            Console.WriteLine($"Writing replication page");
                        var items = new Items();
                        items.Batch.AddRange(batch);
                        await _responseStreamWriteAsync(new SyncPacket
                        {
                            ReplicaId = _ownReplicaId,
                            Items = items
                        });
                        //                            Console.WriteLine($"Replication page has been written");
                    }
                }
                return page.Last().Item1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task WriteAsync(SyncPacket syncPacket) => _responseStreamWriteAsync(syncPacket);
    }
}
