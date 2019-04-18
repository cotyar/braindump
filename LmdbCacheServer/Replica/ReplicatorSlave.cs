using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Utils;
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
    public class ReplicatorSlave : IDisposable
    {
        private readonly LightningPersistence _lmdb;
        private readonly string _ownReplicaId;
        private readonly KvTable _kvTable;
        private readonly ReplicationTable _replicationTable;
        private readonly WriteLogTable _wlTable;
        private readonly ReplicationConfig _replicationConfig;
        private readonly Func<WriteTransaction, string, ulong, VectorClock> _incrementClock;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly string _targetReplicaId;
        private readonly SyncService.SyncServiceClient _syncService;
        private readonly Replicator _replicator;

        private bool _started;

        public ReplicatorSlave(LightningPersistence lmdb, string ownReplicaId, Channel syncChannel, 
            KvTable kvTable, ReplicationTable replicationTable, WriteLogTable wlTable,
            ReplicationConfig replicationConfig, Func<WriteTransaction, string, ulong, VectorClock> incrementClock) // TODO: Add ACKs streaming
        {
            _lmdb = lmdb;
            _ownReplicaId = ownReplicaId;
            _kvTable = kvTable;
            _replicationTable = replicationTable;
            _wlTable = wlTable;
            _replicationConfig = replicationConfig;
            _incrementClock = incrementClock;
            _cancellationTokenSource = new CancellationTokenSource();

            _syncService = new SyncService.SyncServiceClient(syncChannel);
            _targetReplicaId = _syncService.GetReplicaId(new Empty()).ReplicaId; // TODO: Add timeouts
            _replicator = new Replicator(_lmdb, _ownReplicaId, _targetReplicaId,
                _kvTable, _replicationTable, _wlTable, _replicationConfig, _cancellationTokenSource.Token, _incrementClock);
        }

        public async Task StartSync()
        {
            if (_started) throw new Exception("Sync already started");
            _started = true;

            await StartPush();
            await StartPull();
        }

        private async Task<Func<ulong, WriteLogEvent, Task>> StartPush()
        {
            var syncFrom = await _syncService.SyncToAsync(new SyncToRequest { ReplicaId = _ownReplicaId }); // TODO: Add timeouts
            using (var callPublish = _syncService.Publish())
            {
                var itemsCount = 0;
                var replicator = new Replicator(_lmdb, _ownReplicaId, _targetReplicaId,
                    _kvTable, _replicationTable, _wlTable, _replicationConfig, _cancellationTokenSource.Token, _incrementClock,
                    async packet =>
                    {
                        await callPublish.RequestStream.WriteAsync(packet);
                        itemsCount++;
                    });

                var lastPos = syncFrom.Since;
                do
                {
                    itemsCount = 0;
                    await replicator.SyncFrom(lastPos, syncFrom.IncludeMine ? new string[0] : new[] { syncFrom.ReplicaId });
                    if (itemsCount > 0) lastPos++; // avoid repeating the last item
                } while (itemsCount > 0);

                var timingRedundancyTask = Task.Run(() => replicator.SyncFrom(lastPos, syncFrom.IncludeMine ? new string[0] : new[] { syncFrom.ReplicaId }));

                return async (lp, wle) =>
                    {
                        await timingRedundancyTask;
                        await replicator.WriteAsync(new SyncPacket {Item = new Item {Pos = lp, LogEvent = wle}});
                    };
            }
        }

        private async Task StartPull()
        {
            var lastPos = _replicationTable.GetLastPos(_targetReplicaId) + 1 ?? 0;

            int itemsCount;
            do
            {
                (itemsCount, lastPos) = await SyncWriteLog(lastPos);
                if (itemsCount > 0) lastPos++; // avoid repeating the last item
            } while (itemsCount > 0);

            using (var callSubscribe = _syncService.Subscribe(new SyncSubscribeRequest {ReplicaId = _ownReplicaId}))
            {
                await SyncWriteLog(lastPos);

                // NOTE: Don't change the order of Subscribe and SyncFrom as it may break "at-least-one" guarantee
                await callSubscribe.ResponseStream.ForEachAsync(async response =>
                {
                    Console.WriteLine($"Sync Received: '{response}'");
                    lastPos = await _replicator.ProcessSyncPacket(response) ?? lastPos;
                });
            }
        }

        private async Task<(int, ulong)> SyncWriteLog(ulong syncStartPos)
        {
            Console.WriteLine($"Started WriteLog Sync");
            var lastPos = syncStartPos;
            var itemsCount = 0;

            using (var callFrom = _syncService.SyncFrom(
                new SyncFromRequest { ReplicaId = _ownReplicaId, Since = syncStartPos, IncludeMine = false, IncludeAcked = false }))
            {
                await callFrom.ResponseStream.ForEachAsync(async sp =>
                {
                    var newLastPos = await _replicator.ProcessSyncPacket(sp);
                    if (newLastPos.HasValue)
                    {
                        itemsCount++;
                        if (newLastPos.Value > lastPos)
                            lastPos = newLastPos.Value;
                    }
                });
            }
            Console.WriteLine($"Synchronized '{itemsCount}' items");
            return (itemsCount, lastPos);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _replicator.Dispose();
        }
    }
}
