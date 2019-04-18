using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using LmdbCache;
using LmdbCacheServer.Tables;
using LmdbLight;
using static LmdbCache.KvMetadata.Types;
using static LmdbCache.KvMetadata.Types.Status;
using static LmdbCache.KvMetadata.Types.UpdateAction;
using static LmdbCache.SyncPacket.PacketOneofCase;
using static LmdbCache.SyncPacket.Types;
using static LmdbCache.WriteLogEvent.Types;

namespace LmdbCacheServer.Replica
{
    public class ReplicatorSource : IDisposable
    {
        private readonly LightningPersistence _lmdb;
        private readonly string _ownReplicaId;
        private readonly WriteLogTable _wlTable;
        private readonly CancellationToken _cancellationToken;
        private readonly Func<SyncPacket, Task> _responseStreamWriteAsync;

        private readonly ReplicationConfig _replicationConfig;
        private readonly Func<AbstractTransaction, string, ByteString> _getValue;

        public ReplicatorSource(LightningPersistence lmdb, string _ownReplicaId,
            KvTable kvTable, WriteLogTable wlTable,
            ReplicationConfig replicationConfig, CancellationToken cancellationToken,
            Func<SyncPacket, Task> responseStreamWriteAsync)
        {
            _lmdb = lmdb;
            this._ownReplicaId = _ownReplicaId;
            _wlTable = wlTable;
            _replicationConfig = replicationConfig;
            _cancellationToken = cancellationToken;
            _responseStreamWriteAsync = responseStreamWriteAsync;

            _getValue = (txn, key) => kvTable.TryGet(txn, new KvKey(key)).Item2?.Value;
        }

        public void Dispose()
        {
        }

        public async Task SyncFrom(ulong since, string[] excludeOriginReplicas)
        {
            var lastPos = since;
            Console.WriteLine($"Received replication request to sync from {lastPos}");

            if (!_cancellationToken.IsCancellationRequested)
            {
                var page = _lmdb.Read(txn =>
                {
                    var logPage = _wlTable.GetLogPage(txn, lastPos, _replicationConfig.PageSize)
                        .Where(logItem => !excludeOriginReplicas.Contains(logItem.Item2.OriginatorReplicaId))
                        . // TODO: Add check for IncludeAcked
                        ToArray();

                    foreach (var logItem in logPage)
                    {
                        if (logItem.Item2.LoggedEventCase == WriteLogEvent.LoggedEventOneofCase.Updated && (logItem.Item2.Updated.Value == null || logItem.Item2.Updated.Value.IsEmpty))
                        {
                            var latestValue = _getValue(txn, logItem.Item2.Updated.Key);
                            if (latestValue != null)
                            {
                                logItem.Item2.Updated.Value = latestValue; // TODO: Handle "Value not found"
                            }
                            else
                            {
                                Console.WriteLine($"No value for the key {logItem.Item2.Updated.Key}");
                            }
                        }
                    }

                    return logPage;
                });

                if (page.Length > 0)
                {
                    if (_replicationConfig.UseBatching)
                    {
                        await ProcessInBatches(page);
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
                                    Item = new Item { Pos = pos, LogEvent = item }
                                });
                                lastPos = pos;
                            }
                        }
                    }
                }

                await _responseStreamWriteAsync(new SyncPacket
                {
                    ReplicaId = _ownReplicaId,
                    Footer = new Footer { LastPos = lastPos }
                });
            }

            Console.WriteLine($"Page replicated. Last pos: {lastPos}");

            async Task ProcessInBatches((ulong, WriteLogEvent)[] page)
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
                                {new Item {Pos = logItem.Item1, LogEvent = logItem.Item2}});
                            return (itemSize, currentBatches);
                        }

                        currentBatches.Last().Add(new Item {Pos = logItem.Item1, LogEvent = logItem.Item2});
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
                        lastPos = batch.Last().Pos;
                        //                            Console.WriteLine($"Replication page has been written");
                    }
                }
            }
        }

        public Task WriteAsync(SyncPacket syncPacket) => _responseStreamWriteAsync(syncPacket);
    }
}
