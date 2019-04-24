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
        private readonly KvTable _kvTable;
        private readonly ReplicationTable _replicationTable;
        private readonly CancellationToken _cancellationToken;
        private readonly Func<WriteTransaction, string, ulong, VectorClock> _incrementClock;
        private readonly string _targetReplicaId;

        private readonly ReplicationConfig _replicationConfig;

        public ReplicatorSink(LightningPersistence lmdb, string targetReplicaId,
            KvTable kvTable, ReplicationTable replicationTable, 
            ReplicationConfig replicationConfig, CancellationToken cancellationToken,
            Func<WriteTransaction, string, ulong, VectorClock> incrementClock)
        {
            _lmdb = lmdb;
            _targetReplicaId = targetReplicaId;
            _kvTable = kvTable;
            _replicationTable = replicationTable;
            _replicationConfig = replicationConfig;
            _cancellationToken = cancellationToken;
            _incrementClock = incrementClock;
        }

        public async Task<ulong?> ProcessSyncPacket(SyncPacket syncPacket)
        {
            if (_cancellationToken.IsCancellationRequested) return null;

            switch (syncPacket.PacketCase)
            {
                case Items:
                    Console.WriteLine($"Received batch: '{syncPacket.Items.Batch.Count}'");
                    ulong? lastPos = null;
                    foreach (var responseItem in syncPacket.Items.Batch)
                    {
                        if (_cancellationToken.IsCancellationRequested) break;
                        await SyncHandler((responseItem.Pos, responseItem.LogEvent));
                        lastPos = responseItem.Pos;
                    }
                    return lastPos;
                case Item:
                    //                            Console.WriteLine($"Received: '{syncPacket}'");
                    await SyncHandler((syncPacket.Item.Pos, syncPacket.Item.LogEvent));
                    return syncPacket.Item.Pos;
                case SkipPos:
                    await _lmdb.WriteAsync(txn =>
                    {
                        _replicationTable.SetLastPos(txn, _targetReplicaId, syncPacket.SkipPos.LastPos);
                    }, false, true);
                    return syncPacket.SkipPos.LastPos;
                case SyncFrom:
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
                            ValueMetadata = addedOrUpdated.ValueMetadata,
                            CorrelationId = syncEvent.Item2.CorrelationId
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
                            Updated = currentClock,
                            CorrelationId = syncEvent.Item2.CorrelationId,
                            ValueMetadata = null
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
    }
}
