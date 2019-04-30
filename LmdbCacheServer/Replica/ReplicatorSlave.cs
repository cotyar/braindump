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
using static LmdbCache.Helper;
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
        private readonly ConcurrentDictionary<string, (Task<Func<Item, Task>>, Task)> _replicators;
        private readonly ConcurrentDictionary<string, ReplicatorSource> _replicationSources;

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
            _replicators = new ConcurrentDictionary<string, (Task<Func<Item, Task>>, Task)>();
            _replicationSources = new ConcurrentDictionary<string, ReplicatorSource>();
            _cts = new CancellationTokenSource();
        }

        private ReplicatorSource CreateReplicatorSource(Func<SyncPacket, Task> responseStreamWriteAsync) =>
            new ReplicatorSource(_lmdb, _ownReplicaId, _kvTable, _wlTable, _replicationConfig, _cts.Token, responseStreamWriteAsync);

        private ReplicatorSink CreateReplicatorSink(string targetReplicaId) =>
            new ReplicatorSink(_lmdb, targetReplicaId, _kvTable, _replicationTable, _replicationConfig, _cts.Token, _incrementClock);

        public Task StartSync(Channel syncChannel) =>
            GrpcSafeHandler(async () =>
            {
                var syncService = new SyncService.SyncServiceClient(syncChannel);
                var targetReplicaId =
                    (await syncService.GetReplicaIdAsync(new Empty())).ReplicaId; // TODO: Add timeouts

                var call = syncService.Sync();

                var replicatorSource = CreateReplicatorSource(call.RequestStream.WriteAsync);
                _replicationSources.AddOrUpdate(targetReplicaId, replicatorSource,
                    (replicaId, rs) =>
                    {
                        rs.Dispose();
                        return replicatorSource;
                    });

                var replicatorSink = CreateReplicatorSink(targetReplicaId);
                await call.RequestStream.WriteAsync(new SyncPacket
                {
                    ReplicaId = _ownReplicaId,
                    SyncFrom = new SyncFrom
                    {
                        ReplicaId = _ownReplicaId,
                        Since = _replicationTable.GetLastPos(targetReplicaId) + 1 ?? 0,
                        IncludeMine = false,
                        IncludeAcked = true // TODO: Not used yet
                    }
                });

                await call.ResponseStream.ForEachAsync(async syncPacket =>
                {
                    if (syncPacket.PacketCase == SyncPacket.PacketOneofCase.SyncFrom)
                    {

                        var task = Task.Run(() => replicatorSource.SyncFrom(syncPacket.SyncFrom.Since,
                            syncPacket.SyncFrom.IncludeMine ? new string[0] : new[] {syncPacket.ReplicaId}));
                        if (_replicationConfig.AwaitSyncFrom)
                        {
                            await task;
                        }
                    }
                    else
                    {
                        await replicatorSink.ProcessSyncPacket(syncPacket);
                    }
                });
            });

        public Task PostWriteLogEvent(Item syncItem) => 
            Task.WhenAll(_replicators.Values.Select(async rep =>
                {
                    var (s, _) = rep;
                    var sink = await s;
                    await sink(syncItem);
                })); // TODO: Add "write to 'm of n'" support 

        public void Dispose()
        {
            _cts.Cancel();
            //_replicator.Dispose();
        }
    }
}
