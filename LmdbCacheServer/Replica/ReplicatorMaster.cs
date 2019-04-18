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
using LmdbCache;
using LmdbCacheServer.Tables;
using LmdbLight;
using static LmdbCache.SyncPacket.Types;
using static LmdbCache.Helper;

namespace LmdbCacheServer.Replica
{
    public class ReplicatorMaster : SyncService.SyncServiceBase, IDisposable
    {
        private readonly LightningPersistence _lmdb;
        private readonly string _ownReplicaId;
        private readonly KvTable _kvTable;
        private readonly ReplicationTable _replicationTable;
        private readonly WriteLogTable _wlTable;
        private readonly ReplicationConfig _replicationConfig;
        private readonly Func<WriteTransaction, string, ulong, VectorClock> _incrementClock;
        private readonly ConcurrentDictionary<string, (Replicator, TaskCompletionSource<bool>)> _slaves;
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
            _slaves = new ConcurrentDictionary<string, (Replicator, TaskCompletionSource<bool>)>();

        }

        private Replicator CreateReplicator(string targetReplicaId, Func<SyncPacket, Task> responseStreamWriteAsync) =>
            new Replicator(_lmdb, _ownReplicaId, targetReplicaId,
                _kvTable, _replicationTable, _wlTable, _replicationConfig, _cts.Token, _incrementClock, responseStreamWriteAsync);

        public override Task<GetReplicaIdResponse> GetReplicaId(Empty request, ServerCallContext context) => 
            Task.FromResult(new GetReplicaIdResponse { ReplicaId = _ownReplicaId} );

        public override Task SyncFrom(SyncFromRequest request, IServerStreamWriter<SyncPacket> responseStream, ServerCallContext context) =>
            GrpcSafeHandler(async () =>
            {
                var replicator = CreateReplicator(request.ReplicaId, responseStream.WriteAsync);
                await replicator.SyncFrom(request.Since, request.IncludeMine ? new string[0] : new []{ request.ReplicaId });
            });

        public override Task<Empty> Ack(IAsyncStreamReader<SyncAckRequest> requestStream, ServerCallContext context)
        {
            return base.Ack(requestStream, context); // TODO: Override when state replication is implemented
        }

        public override Task Subscribe(SyncSubscribeRequest request, IServerStreamWriter<SyncPacket> responseStream, ServerCallContext context) =>
            GrpcSafeHandler(async () =>
            {
                var tcs = new TaskCompletionSource<bool>();
                var replicator = CreateReplicator(request.ReplicaId, responseStream.WriteAsync);
                _slaves.AddOrUpdate(request.ReplicaId, (replicator, tcs), (s, writer) => throw new ReplicationIdUsedException(request.ReplicaId));

                await tcs.Task;
            });

        public Task PostWriteLogEvent(ulong pos, WriteLogEvent wle) => 
            Task.WhenAll(_slaves.Values.Select(slave => slave.Item1.WriteAsync(new SyncPacket { Item = new Item { Pos = pos, LogEvent = wle } }))); // TODO: Add "write to 'm of n'" support 

        public Task TerminateSync(string replicaId)
        {
            if (_slaves.TryRemove(replicaId, out var streamWriter))
            {
                streamWriter.Item1.Dispose();
                streamWriter.Item2.SetResult(true);
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            foreach (var (_, (replicator, tcs)) in _slaves)
            {
                replicator.Dispose();
                tcs.SetResult(true);
            }
        }
    }
}
