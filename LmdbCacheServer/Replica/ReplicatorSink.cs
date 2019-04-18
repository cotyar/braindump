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
using static LmdbCache.WriteLogEvent.Types;

namespace LmdbCacheServer.Replica
{
    public class ReplicatorSink : IDisposable
    {
        private readonly LightningPersistence _lmdb;
        private readonly string _ownReplicaId;
        private readonly KvTable _kvTable;
        private readonly ReplicationTable _replicationTable;
        private readonly WriteLogTable _wlTable;
        private readonly CancellationToken _cancellationToken;
        private readonly Func<WriteTransaction, string, ulong, VectorClock> _incrementClock;
        private readonly Func<SyncPacket, Task> _responseStreamWriteAsync;
        private readonly string _targetReplicaId;

        private readonly ReplicationConfig _replicationConfig;
        private readonly Func<AbstractTransaction, string, ByteString> _getValue;

        public ReplicatorSink(LightningPersistence lmdb, string ownReplicaId, string targetReplicaId,
            KvTable kvTable, ReplicationTable replicationTable, WriteLogTable wlTable,
            ReplicationConfig replicationConfig, CancellationToken cancellationToken,
            Func<WriteTransaction, string, ulong, VectorClock> incrementClock, Func<SyncPacket, Task> responseStreamWriteAsync)
        {
            _lmdb = lmdb;
            _ownReplicaId = ownReplicaId;
            _targetReplicaId = targetReplicaId;
            _kvTable = kvTable;
            _replicationTable = replicationTable;
            _wlTable = wlTable;
            _replicationConfig = replicationConfig;
            _cancellationToken = cancellationToken;
            _incrementClock = incrementClock;
            _responseStreamWriteAsync = responseStreamWriteAsync;

            _getValue = (txn, key) => _kvTable.TryGet(txn, new KvKey(key)).Item2?.Value;
        }

        public async Task<ulong?> ProcessSyncPacket(SyncPacket response)
        {
            if (_cancellationToken.IsCancellationRequested) return null;

            switch (response.PacketCase)
            {
                case Items:
                    Console.WriteLine($"Received batch: '{response.Items.Batch.Count}'");
                    ulong? lastPos = null;
                    foreach (var responseItem in response.Items.Batch)
                    {
                        if (_cancellationToken.IsCancellationRequested) break;
                        await SyncHandler((responseItem.Pos, responseItem.LogEvent));
                        lastPos = responseItem.Pos;
                    }
                    return lastPos;
                case Item:
                    //                            Console.WriteLine($"Received: '{response}'");
                    await SyncHandler((response.Item.Pos, response.Item.LogEvent));
                    return response.Item.Pos;
                case Footer:
                    await _lmdb.WriteAsync(txn =>
                    {
                        _replicationTable.SetLastPos(txn, _targetReplicaId, response.Footer.LastPos);
                    }, false, true);
                    return response.Footer.LastPos;
                case None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task SyncHandler((ulong, WriteLogEvent) syncEvent)
        {
            if (_cancellationToken.IsCancellationRequested) return;

            switch (syncEvent.Item2.LoggedEventCase)
            {
                case WriteLogEvent.LoggedEventOneofCase.Updated:
                    var addedOrUpdated = syncEvent.Item2.Updated;
                    await _lmdb.WriteAsync(txn =>
                    {
                        var addMetadata = new KvMetadata
                        {
                            Status = Active,
                            Expiry = addedOrUpdated.Expiry,
                            Action = Replicated,
                            Updated = _incrementClock(txn, _targetReplicaId, syncEvent.Item1),
                            Compression = KvMetadata.Types.Compression.None // TODO: Use correct compression mode
                        };

                        var wasUpdated = _kvTable.Add(
                            txn,
                            new KvKey(addedOrUpdated.Key),
                            addMetadata,
                            new KvValue(addedOrUpdated.Value),
                            addMetadata.Updated,
                            (key, vcOld, vcNew) => vcOld.Earlier(vcNew));
                        _replicationTable.SetLastPos(txn, _targetReplicaId, syncEvent.Item1);
                        _kvTable.StatusTable.IncrementCounters(txn, replicatedAdds: 1);
                        // TODO: Should we do anything if the value wasn't updated? Maybe logging?        
                    }, false, true);
                    break;
                case WriteLogEvent.LoggedEventOneofCase.Deleted:
                    var deleted = syncEvent.Item2.Deleted;
                    await _lmdb.WriteAsync(txn =>
                    {
                        var currentClock = _incrementClock(txn, _targetReplicaId, syncEvent.Item1);
                        var delMetadata = new KvMetadata
                        {
                            Status = KvMetadata.Types.Status.Deleted,
                            Expiry = currentClock.TicksOffsetUtc.ToTimestamp(),
                            Action = Replicated,
                            Updated = currentClock
                        };

                        var kvKey = new KvKey(deleted.Key);
                        _kvTable.Delete(txn, kvKey, delMetadata);
                        _replicationTable.SetLastPos(txn, _targetReplicaId, syncEvent.Item1);
                        _kvTable.StatusTable.IncrementCounters(txn, replicatedDeletes: 1);
                        // TODO: Should we do anything if the value wasn't updated? Maybe logging?
                    }, false, true);
                    break;
                case WriteLogEvent.LoggedEventOneofCase.None:
                    throw new ArgumentException("syncEvent", $"Unexpected LogEvent case: {syncEvent.Item2.LoggedEventCase}");
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
                                await _responseStreamWriteAsync(new SyncPacket { Item = new SyncPacket.Types.Item { Pos = pos, LogEvent = item } });
                                lastPos = pos;
                            }
                        }
                    }
                }

                await _responseStreamWriteAsync(new SyncPacket { Footer = new SyncPacket.Types.Footer { LastPos = lastPos } });
            }

            Console.WriteLine($"Page replicated. Last pos: {lastPos}");

            async Task ProcessInBatches((ulong, WriteLogEvent)[] page)
            {
                var batches = page.Aggregate((0, new List<List<SyncPacket.Types.Item>>()), (acc, logItem) =>
                    {
                        var (lastBatchBytes, currentBatches) = acc;
                        var itemSize = logItem.Item2.LoggedEventCase == WriteLogEvent.LoggedEventOneofCase.Updated
                            ? logItem.Item2.Updated.Value.Length
                            : 100;
                        if (currentBatches.Count == 0 || lastBatchBytes + itemSize > 3 * 1024 * 1024)
                        {
                            currentBatches.Add(new List<SyncPacket.Types.Item>
                                {new SyncPacket.Types.Item {Pos = logItem.Item1, LogEvent = logItem.Item2}});
                            return (itemSize, currentBatches);
                        }

                        currentBatches.Last().Add(new SyncPacket.Types.Item {Pos = logItem.Item1, LogEvent = logItem.Item2});
                        return (lastBatchBytes + itemSize, currentBatches);
                    })
                    .Item2;

                foreach (var batch in batches)
                {
                    if (!_cancellationToken.IsCancellationRequested)
                    {
                        //                            Console.WriteLine($"Writing replication page");
                        var items = new SyncPacket.Types.Items();
                        items.Batch.AddRange(batch);
                        await _responseStreamWriteAsync(new SyncPacket {Items = items});
                        lastPos = batch.Last().Pos;
                        //                            Console.WriteLine($"Replication page has been written");
                    }
                }
            }
        }

        public Task WriteAsync(SyncPacket syncPacket) => _responseStreamWriteAsync(syncPacket);
    }
}
