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
using NLog;
using static LmdbCache.KvMetadata.Types;
using static LmdbCache.KvMetadata.Types.Status;
using static LmdbCache.KvMetadata.Types.UpdateAction;
using static LmdbCache.SyncPacket.PacketOneofCase;
using static LmdbCache.WriteLogEvent.Types;

namespace LmdbCacheServer.Replica
{
    public class ReplicatorSink : IDisposable
    {
        private Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private readonly LightningPersistence _lmdb;
        private readonly KvTable _kvTable;
        private readonly ReplicationTable _replicationTable;
        private readonly CancellationToken _cancellationToken;
        private readonly Action<WriteTransaction, string, ulong?> _updateClock;
        private readonly string _targetReplicaId;

        private readonly ReplicationConfig _replicationConfig;

        public ReplicatorSink(LightningPersistence lmdb, string targetReplicaId,
            KvTable kvTable, ReplicationTable replicationTable, 
            ReplicationConfig replicationConfig, CancellationToken cancellationToken,
            Action<WriteTransaction, string, ulong?> updateClock)
        {
            _lmdb = lmdb;
            _targetReplicaId = targetReplicaId;
            _kvTable = kvTable;
            _replicationTable = replicationTable;
            _replicationConfig = replicationConfig;
            _cancellationToken = cancellationToken;
            _updateClock = updateClock;
        }

        public async Task<ulong?> ProcessSyncPacket(SyncPacket syncPacket)
        {
            if (_cancellationToken.IsCancellationRequested) return null;

            switch (syncPacket.PacketCase)
            {
                case Items:
                    _log.Info($"Received batch: '{syncPacket.Items.Batch.Count}'");
                    ulong? lastPos = null;
                    foreach (var responseItem in syncPacket.Items.Batch)
                    {
                        if (_cancellationToken.IsCancellationRequested) break;
                        var pos = await SyncHandler(responseItem.LogEvent);
                        lastPos = pos;
                    }
                    return lastPos;
                case Item:
                    //                            Console.WriteLine($"Received: '{syncPacket}'");
                    return await SyncHandler(syncPacket.Item.LogEvent);
                case SkipPos:
                    await _lmdb.WriteAsync(txn =>
                    {
                        _replicationTable.SetLastPos(txn, _targetReplicaId, syncPacket.SkipPos.LastPos);
                        _updateClock(txn, _targetReplicaId, syncPacket.SkipPos.LastPos);
                    }, false, true);
                    return syncPacket.SkipPos.LastPos;
                case SyncFrom:
                case None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task<ulong> SyncHandler(WriteLogEvent syncEvent)
        {
            var posn = syncEvent.LocallySaved.GetReplicaValue(_targetReplicaId);
            if (!posn.HasValue) throw new EventLogException($"Broken syncEvent.LocallySaved during replication '{syncEvent.LocallySaved}'. For replica '{_targetReplicaId}'");
            var pos = posn.Value;

            if (_cancellationToken.IsCancellationRequested) return pos;

            switch (syncEvent.LoggedEventCase)
            {
                case WriteLogEvent.LoggedEventOneofCase.Updated:
                    var addedOrUpdated = syncEvent.Updated;
                    await _lmdb.WriteAsync(txn =>
                    {
                        var addMetadata = new KvMetadata
                        {
                            Status = Active,
                            Expiry = addedOrUpdated.Expiry,
                            Action = Replicated,
                            Originated = syncEvent.Originated,
                            ValueMetadata = syncEvent.ValueMetadata,
                            CorrelationId = syncEvent.CorrelationId
                        };

                        var wasUpdated = _kvTable.Add(
                            txn,
                            new KvKey(addedOrUpdated.Key),
                            addMetadata,
                            new KvValue(addedOrUpdated.Value),
                            (key, vcOld, vcNew) => vcOld.Earlier(vcNew));
                        _replicationTable.SetLastPos(txn, _targetReplicaId, pos);
                        _kvTable.StatusTable.IncrementCounters(txn, replicatedAdds: 1);
                        // TODO: Should we do anything if the value wasn't updated? Maybe logging?        
                    }, false, true);
                    break;
                case WriteLogEvent.LoggedEventOneofCase.Deleted:
                    var deleted = syncEvent.Deleted;
                    await _lmdb.WriteAsync(txn =>
                    {
                        _updateClock(txn, _targetReplicaId, syncEvent.LocallySaved.GetReplicaValue(_targetReplicaId));
                        var delMetadata = new KvMetadata
                        {
                            Status = KvMetadata.Types.Status.Deleted,
                            Expiry = syncEvent.LocallySaved.TicksOffsetUtc.ToTimestamp(),
                            Action = Replicated,
                            Originated = syncEvent.Originated,
                            OriginatorReplicaId = syncEvent.OriginatorReplicaId,
                            CorrelationId = syncEvent.CorrelationId,
                            ValueMetadata = syncEvent.ValueMetadata,
                        };

                        var kvKey = new KvKey(deleted.Key);
                        _kvTable.Delete(txn, kvKey, delMetadata);
                        _replicationTable.SetLastPos(txn, _targetReplicaId, pos);
                        _kvTable.StatusTable.IncrementCounters(txn, replicatedDeletes: 1);
                        // TODO: Should we do anything if the value wasn't updated? Maybe logging?
                    }, false, true);
                    break;
                case WriteLogEvent.LoggedEventOneofCase.None:
                    throw new ArgumentException("syncEvent", $"Unexpected LogEvent case: {syncEvent.LoggedEventCase}");
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return pos;
        }

        public void Dispose()
        {
        }
    }
}
