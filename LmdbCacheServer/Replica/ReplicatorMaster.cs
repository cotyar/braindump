using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncEnumerableExtensions;
using Grpc.Core;
using LmdbCache;
using LmdbLight;

namespace LmdbCacheServer.Replica
{
    public class ReplicatorMaster : SyncService.SyncServiceBase, IDisposable
    {
        private readonly LightningPersistence _lmdb;
        private readonly string _ownReplicaId;
        private readonly WriteLogTable _wlTable;
        private readonly uint _syncPageSize;
        private readonly ConcurrentDictionary<string, (IServerStreamWriter<SyncSubscribeResponse>, TaskCompletionSource<bool>)> _slaves;
        private readonly CancellationTokenSource _cts;

        public ReplicatorMaster(LightningPersistence lmdb, string ownReplicaId, WriteLogTable wlTable, uint syncPageSize)
        {
            _lmdb = lmdb;
            _ownReplicaId = ownReplicaId;
            _wlTable = wlTable;
            _syncPageSize = syncPageSize;
            _cts = new CancellationTokenSource();
            _slaves = new ConcurrentDictionary<string, (IServerStreamWriter<SyncSubscribeResponse>, TaskCompletionSource<bool>)>();
        }

        public override Task<GetReplicaIdResponse> GetReplicaId(Empty request, ServerCallContext context) => 
            Task.FromResult(new GetReplicaIdResponse { ReplicaId = _ownReplicaId} );

        public override async Task SyncFrom(SyncFromRequest request, IServerStreamWriter<SyncFromResponse> responseStream, ServerCallContext context)
        {
            var lastPos = request.Since;
            while (!_cts.Token.IsCancellationRequested)
            {
                var page = _lmdb.Read(txn => _wlTable.GetLogPage(txn, request.Since, _syncPageSize));

                if (page.Length == 0) break;

                foreach (var wlEvent in page)
                {
                    if (!_cts.Token.IsCancellationRequested
                        && (request.IncludeMine || wlEvent.Item2.OriginatorReplicaId != _ownReplicaId)) // TODO: Add check for IncludeAcked
                        await responseStream.WriteAsync(new SyncFromResponse { Item = { Pos = wlEvent.Item1, LogEvent = wlEvent.Item2 }});
                    lastPos = wlEvent.Item1;
                }

                await responseStream.WriteAsync(new SyncFromResponse { Footer = { LastPos = lastPos } });
            }
        }

        public override Task<Empty> Ack(IAsyncStreamReader<SyncAckRequest> requestStream, ServerCallContext context)
        {
            return base.Ack(requestStream, context); // TODO: Override when state replication is implemented
        }

        public override async Task Subscribe(SyncSubscribeRequest request, IServerStreamWriter<SyncSubscribeResponse> responseStream, ServerCallContext context)
        {
            var tcs = new TaskCompletionSource<bool>();
            _slaves.AddOrUpdate(request.ReplicaId, (responseStream, tcs),
                (s, writer) => throw new ReplicationIdUsedException(request.ReplicaId));

            await tcs.Task;
        }

        public Task PostWriteLogEvent(ulong pos, WriteLogEvent wle) => 
            Task.WhenAll(_slaves.Values.Select(slave => slave.Item1.WriteAsync(new SyncSubscribeResponse { LogEvent = wle })));

        public Task TerminateSync(string replicaId)
        {
            if (_slaves.TryRemove(replicaId, out var streamWriter))
            {
                streamWriter.Item2.SetResult(true);
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            foreach (var slave in _slaves)
            {
                slave.Value.Item2.SetResult(true);
            }
        }
    }
}
