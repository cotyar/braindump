using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using LmdbCache;

namespace LmdbCacheServer.Replica
{
    public class Replicator : IDisposable
    {
        private readonly string _ownReplicaId;
        private readonly string _targetReplicaId;
        private readonly Func<string, VectorClock> _sinceClockForReplica;
        private readonly SyncService.SyncServiceClient _syncService;

        public Replicator(string ownReplicaId, Channel syncChannel, /*string targetReplicaId, */ Func<string, VectorClock> sinceClockForReplica) // TODO: Add ACKs streaming
        {
            _ownReplicaId = ownReplicaId;
            _sinceClockForReplica = sinceClockForReplica;

            _syncService = new SyncService.SyncServiceClient(syncChannel);
            _targetReplicaId = _syncService.GetReplicaId(new Empty()).ReplicaId; // TODO: Add timeouts
        }

        public async Task StartSync(Func<SyncResponse, Task> syncHandler)
        {
            using (var call = _syncService.Subscribe(new SyncSubscribeRequest { ReplicaId = _targetReplicaId, Since = _sinceClockForReplica(_targetReplicaId) }))
            {
                while (await call.ResponseStream.MoveNext())
                {
                    var feature = call.ResponseStream.Current;
                    Console.WriteLine($"Received: '{feature}'");
                    await syncHandler(feature);
                }
            }
        }

        public void Dispose()
        {
            
        }
    }
}
