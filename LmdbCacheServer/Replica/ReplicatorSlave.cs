using System;
using System.Collections.Concurrent;
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
    public class ReplicatorSlave : IReplicator, IDisposable
    {
        private readonly LightningPersistence _lmdb;
        private readonly string _ownReplicaId;
        private readonly KvTable _kvTable;
        private readonly ReplicationTable _replicationTable;
        private readonly WriteLogTable _wlTable;
        private readonly ReplicationConfig _replicationConfig;
        private readonly Func<WriteTransaction, string, ulong, VectorClock> _incrementClock;
        private readonly CancellationTokenSource _cts;
        private readonly ConcurrentDictionary<string, (Task<Func<ulong, WriteLogEvent, Task>>, Task)> _replicators;

        public ReplicatorSlave(LightningPersistence lmdb, string ownReplicaId, 
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
            _replicators = new ConcurrentDictionary<string, (Task<Func<ulong, WriteLogEvent, Task>>, Task)>();
            _cts = new CancellationTokenSource();
        }

        private ReplicatorSource CreateReplicatorSource(Func<SyncPacket, Task> responseStreamWriteAsync) =>
            new ReplicatorSource(_lmdb, _ownReplicaId, _kvTable, _wlTable, _replicationConfig, _cts.Token, responseStreamWriteAsync);

        private ReplicatorSink CreateReplicatorSink(string targetReplicaId) =>
            new ReplicatorSink(_lmdb, targetReplicaId, _kvTable, _replicationTable, _replicationConfig, _cts.Token, _incrementClock);

        public async Task<bool> StartSync(Channel syncChannel)
        {
            var syncService = new SyncService.SyncServiceClient(syncChannel);
            var targetReplicaId = (await syncService.GetReplicaIdAsync(new Empty())).ReplicaId; // TODO: Add timeouts

            bool added = false;
            // TODO: Assuming not many concurrent StartSync requests
            _replicators.AddOrUpdate(targetReplicaId, _ =>
                {
                    added = true;
                    return (StartPush(syncService), StartPull(syncService, targetReplicaId));
                }, (_, v) =>
                {
                    added = false;
                    return v;
                }
            );

            return added;
        }

        private async Task<Func<ulong, WriteLogEvent, Task>> StartPush(SyncService.SyncServiceClient syncService)
        {
            var syncFrom = await syncService.SyncToAsync(new SyncToRequest { ReplicaId = _ownReplicaId }); // TODO: Add timeouts
            var callPublish = syncService.Publish();

            var itemsCount = 0;
            var replicatorSource = CreateReplicatorSource(async packet =>
                {
                    await callPublish.RequestStream.WriteAsync(packet);
                    itemsCount++;
                });

            var lastPos = syncFrom.Since;
            do
            {
                itemsCount = 0;
                await replicatorSource.SyncFrom(lastPos, syncFrom.IncludeMine ? new string[0] : new[] { syncFrom.ReplicaId });
                if (itemsCount > 0) lastPos++; // avoid repeating the last item
            } while (itemsCount > 0);

            var timingRedundancyTask = Task.Run(() => replicatorSource.SyncFrom(lastPos, syncFrom.IncludeMine ? new string[0] : new[] { syncFrom.ReplicaId }));

            return async (lp, wle) =>
                {
                    await timingRedundancyTask;
                    await replicatorSource.WriteAsync(new SyncPacket
                    {
                        ReplicaId = _ownReplicaId,
                        Item = new Item {Pos = lp, LogEvent = wle}
                    });
                };            
        }

        private async Task StartPull(SyncService.SyncServiceClient syncService, string targetReplicaId)
        {
            var replicatorSink = CreateReplicatorSink(targetReplicaId);
            var lastPos = _replicationTable.GetLastPos(targetReplicaId) + 1 ?? 0;

            int itemsCount;
            do
            {
                (itemsCount, lastPos) = await SyncWriteLog(syncService, replicatorSink, lastPos);
                if (itemsCount > 0) lastPos++; // avoid repeating the last item
            } while (itemsCount > 0);

            using (var callSubscribe = syncService.Subscribe(new SyncSubscribeRequest {ReplicaId = _ownReplicaId}))
            {
                await SyncWriteLog(syncService, replicatorSink, lastPos);

                // NOTE: Don't change the order of Subscribe and SyncFrom as it may break "at-least-one" guarantee
                await callSubscribe.ResponseStream.ForEachAsync(async response =>
                {
                    Console.WriteLine($"Sync Received: '{response}'");
                    lastPos = await replicatorSink.ProcessSyncPacket(response) ?? lastPos;
                });
            }
        }

        private async Task<(int, ulong)> SyncWriteLog(SyncService.SyncServiceClient syncService,
            ReplicatorSink replicatorSink, ulong syncStartPos)
        {
            Console.WriteLine($"Started WriteLog Sync");
            var lastPos = syncStartPos;
            var itemsCount = 0;

            using (var callFrom = syncService.SyncFrom(
                new SyncFromRequest { ReplicaId = _ownReplicaId, Since = syncStartPos, IncludeMine = false, IncludeAcked = false }))
            {
                await callFrom.ResponseStream.ForEachAsync(async sp =>
                {
                    var newLastPos = await replicatorSink.ProcessSyncPacket(sp);
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

        public Task PostWriteLogEvent(ulong pos, WriteLogEvent wle) => 
            Task.WhenAll(_replicators.Values.Select(async rep =>
            {
                var (s, _) = rep;
                var sink = await s;
                await sink(pos, wle);
            })); // TODO: Add "write to 'm of n'" support 

        public void Dispose()
        {
            _cts.Cancel();
            //_replicator.Dispose();
        }
    }
}
