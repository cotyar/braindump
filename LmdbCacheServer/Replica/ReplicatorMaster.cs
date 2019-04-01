using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncEnumerableExtensions;
using Grpc.Core;
using LmdbCache;
using LmdbLight;

namespace LmdbCacheServer.Replica
{
    public class ReplicatorMaster : SyncService.SyncServiceBase
    {
        private readonly LightningPersistence _lmdb;
        private readonly string _ownReplicaId;
        private readonly WriteLogTable _wlTable;
        private readonly uint _syncPageSize;
        private readonly ConcurrentDictionary<string, (Func<SyncResponse, Task<bool>>, CancellationTokenSource)> _slaves;

        public ReplicatorMaster(LightningPersistence lmdb, string ownReplicaId, WriteLogTable wlTable, uint syncPageSize)
        {
            _lmdb = lmdb;
            _ownReplicaId = ownReplicaId;
            _wlTable = wlTable;
            _syncPageSize = syncPageSize;
        }

        public override Task<GetReplicaIdResponse> GetReplicaId(Empty request, ServerCallContext context) => 
            Task.FromResult(new GetReplicaIdResponse { ReplicaId = _ownReplicaId} );

        public override async Task Subscribe(SyncSubscribeRequest request, IServerStreamWriter<SyncResponse> responseStream, ServerCallContext context)
        {
            var tcs = new CancellationTokenSource();

            async Task SyncLog()
            {
                while (tcs.Token.IsCancellationRequested)
                {
                    var page = _lmdb.Read(txn => _wlTable.GetLogPage(txn, request.Since.GetReplicaValue(_ownReplicaId) ?? 0, _syncPageSize));

                    if (page.Length == 0) break;

                    foreach (var wlEvent in page)
                    {
                        if (!tcs.Token.IsCancellationRequested)
                            await responseStream.WriteAsync(new SyncResponse { LogEvent = wlEvent.Item2 });
                    }
                }
            }

            async Task Generator(IAsyncEnumerableSink<SyncResponse> sink)
            {
                while (true)
                {
                    var page = _lmdb.Read(txn => _wlTable.GetLogPage(txn, request.Since.GetReplicaValue(_ownReplicaId) ?? 0, _syncPageSize));

                    if (page.Length == 0) break;

                    foreach (var wlEvent in page)
                    {
                        await responseStream.WriteAsync(new SyncResponse { LogEvent = wlEvent.Item2 });
                    }
                } 
            }

            _slaves.TryAdd(request.ReplicaId, (async sr =>
                {
                    if (tcs.Token.IsCancellationRequested) return false;

                    await responseStream.WriteAsync(sr);
                    return true;
                }
                , tcs)); // TODO: Add waiting for sync

            var responses = AsyncEnumerableFactory.FromAsyncGenerator<SyncResponse>(Generator).GetEnumerator();
            while (await responses.MoveNext())
            {
                await responseStream.WriteAsync(responses.Current);
            }
        }

        public async Task<bool> PostWriteLogEvent(string replicaId, WriteLogEvent wle)
        {
            if (_slaves.TryGetValue(replicaId, out var streamWriter)) // TODO: Throw exception instead?
            {
                return await streamWriter.Item1(new SyncResponse { LogEvent = wle }); // TODO: Add awaiting for ACKs
            }

            return false;
        }

        public Task TerminateSync(string replicaId)
        {
            if (_slaves.TryRemove(replicaId, out var streamWriter))
            {
                streamWriter.Item2.Cancel();
            }

            return Task.CompletedTask;
        }
    }
}
