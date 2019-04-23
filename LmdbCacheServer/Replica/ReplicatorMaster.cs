using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncEnumerableExtensions;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Utils;
using LmdbCache;
using LmdbCacheServer.Tables;
using LmdbLight;
using static LmdbCache.SyncPacket.Types;
using static LmdbCache.Helper;

namespace LmdbCacheServer.Replica
{
    public class ReplicatorMaster : SyncService.SyncServiceBase, IDisposable, IReplicator
    {
        private readonly LightningPersistence _lmdb;
        private readonly string _ownReplicaId;
        private readonly KvTable _kvTable;
        private readonly ReplicationTable _replicationTable;
        private readonly WriteLogTable _wlTable;
        private readonly ReplicationConfig _replicationConfig;
        private readonly Func<WriteTransaction, string, ulong, VectorClock> _incrementClock;
        private readonly ConcurrentDictionary<string, ReplicatorSource> _replicationSources;
        private readonly ConcurrentDictionary<string, (ReplicatorSink, TaskCompletionSource<bool>)> _replicationSinks;
        private readonly CancellationTokenSource _cts;

        public ReplicatorMaster(LightningPersistence lmdb, string ownReplicaId, 
            KvTable kvTable, ReplicationTable replicationTable, WriteLogTable wlTable,
            ReplicationConfig replicationConfig, Func<WriteTransaction, string, ulong, VectorClock> incrementClock)
        {
            _lmdb = lmdb;
            _ownReplicaId = ownReplicaId;
            _kvTable = kvTable;
            _replicationTable = replicationTable;
            _wlTable = wlTable;
            _replicationConfig = replicationConfig;
            _incrementClock = incrementClock;
            _cts = new CancellationTokenSource();
            _replicationSources = new ConcurrentDictionary<string, ReplicatorSource>();

        }

        private ReplicatorSource CreateReplicatorSource(Func<SyncPacket, Task> responseStreamWriteAsync) =>
            new ReplicatorSource(_lmdb, _ownReplicaId, _kvTable, _wlTable, _replicationConfig, _cts.Token, responseStreamWriteAsync);

        private ReplicatorSink CreateReplicatorSink(string targetReplicaId) =>
            new ReplicatorSink(_lmdb, targetReplicaId, _kvTable, _replicationTable, _replicationConfig, _cts.Token, _incrementClock);

        public override Task<GetReplicaIdResponse> GetReplicaId(Empty request, ServerCallContext context) => 
            Task.FromResult(new GetReplicaIdResponse { ReplicaId = _ownReplicaId} );

        public override Task Sync(IAsyncStreamReader<SyncPacket> requestStream, IServerStreamWriter<SyncPacket> responseStream, ServerCallContext context) =>
            GrpcSafeHandler(async () =>
            {
                ReplicatorSink replicatorSink = null;
                var replicatorSource = CreateReplicatorSource(responseStream.WriteAsync);

                await requestStream.ForEachAsync(async syncPacket =>
                {
                    if (replicatorSink == null)
                    {
                        _replicationSources.AddOrUpdate(syncPacket.ReplicaId, replicatorSource,
                            (replicaId, rs) =>
                            {
                                rs.Dispose();
                                return replicatorSource;
                            });

                        replicatorSink = CreateReplicatorSink(syncPacket.ReplicaId);
                        await responseStream.WriteAsync(new SyncPacket
                        {
                            ReplicaId = _ownReplicaId,
                            SyncFrom = new SyncFrom
                            {
                                ReplicaId = _ownReplicaId,
                                Since = _replicationTable.GetLastPos(syncPacket.ReplicaId) + 1 ?? 0,
                                IncludeMine = false,
                                IncludeAcked = true // TODO: Not used yet
                            }
                        });
                    }

                    if (syncPacket.PacketCase == SyncPacket.PacketOneofCase.SyncFrom)
                    {
                        var task = Task.Run(() => replicatorSource.SyncFrom(syncPacket.SyncFrom.Since, syncPacket.SyncFrom.IncludeMine ? new string[0] : new[] {syncPacket.SyncFrom.ReplicaId }));
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


        //        public override Task SyncFrom(SyncFromRequest request, IServerStreamWriter<SyncPacket> responseStream, ServerCallContext context) =>
        //            GrpcSafeHandler(async () =>
        //            {
        //                var replicator = CreateReplicatorSource(responseStream.WriteAsync);
        //                await replicator.SyncFrom(request.Since, request.IncludeMine ? new string[0] : new []{ request.ReplicaId });
        //            });
        //
        //        public override Task<SyncFromRequest> SyncTo(SyncToRequest request, ServerCallContext context) =>
        //            GrpcSafeHandler(() => new SyncFromRequest
        //            {
        //                ReplicaId = _ownReplicaId,
        //                Since = _replicationTable.GetLastPos(request.ReplicaId) + 1 ?? 0,
        //                IncludeMine = false,
        //                IncludeAcked = true // TODO: Not used yet
        //            });
        //
        //        public override Task<Empty> Publish(IAsyncStreamReader<SyncPacket> requestStream, ServerCallContext context) =>
        //            GrpcSafeHandler(async () =>
        //            {
        //                ReplicatorSink replicator = null;
        //                await requestStream.ForEachAsync(async syncPacket =>
        //                {
        //                    if (replicator == null)
        //                    {
        //                        replicator = CreateReplicatorSink(syncPacket.ReplicaId);
        //                    }
        //
        //                    await replicator.ProcessSyncPacket(syncPacket);
        //                });
        //
        //                return new Empty();
        //            });
        //    
        //
        //        public override Task Subscribe(SyncSubscribeRequest request, IServerStreamWriter<SyncPacket> responseStream, ServerCallContext context) =>
        //            GrpcSafeHandler(async () =>
        //            {
        //                var tcs = new TaskCompletionSource<bool>();
        //                var replicator = CreateReplicatorSource(responseStream.WriteAsync);
        //                _replicationSources.AddOrUpdate(request.ReplicaId, (replicator, tcs), (s, writer) => throw new ReplicationIdUsedException(request.ReplicaId));
        //
        //                await tcs.Task;
        //            });

        public Task PostWriteLogEvent(ulong pos, WriteLogEvent wle) => 
            Task.WhenAll(_replicationSources.Values.Select(slave => slave.WriteAsync(new SyncPacket
            {
                ReplicaId = _ownReplicaId,
                Item = new Item { Pos = pos, LogEvent = wle }
            }))); // TODO: Add "write to 'm of n'" support 

        public bool TerminateSync(string replicaId)
        {
            if (_replicationSources.TryRemove(replicaId, out var streamWriter))
            {
                streamWriter.Dispose();
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            foreach (var (_, replicator) in _replicationSources)
            {
                replicator.Dispose();
            }
        }

//        public override Task<Empty> Ack(IAsyncStreamReader<SyncAckRequest> requestStream, ServerCallContext context)
//        {
//            return base.Ack(requestStream, context); // TODO: Override when state replication is implemented
//        }
    }
}
