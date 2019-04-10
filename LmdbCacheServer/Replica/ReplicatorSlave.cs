using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using LmdbCache;
using LmdbLight;

namespace LmdbCacheServer.Replica
{
    public class ReplicatorSlave : IDisposable
    {
        private readonly string _ownReplicaId;
        private readonly string _targetReplicaId;
        private readonly Func<string, long> _lastPosForReplica;
        private readonly SyncService.SyncServiceClient _syncService;

        public ReplicatorSlave(string ownReplicaId, Channel syncChannel, /*string targetReplicaId, */ Func<string, long> lastPosForReplica) // TODO: Add ACKs streaming
        {
            _ownReplicaId = ownReplicaId;
            _lastPosForReplica = lastPosForReplica;

            _syncService = new SyncService.SyncServiceClient(syncChannel);
            _targetReplicaId = _syncService.GetReplicaId(new Empty()).ReplicaId; // TODO: Add timeouts
        }

        public async Task StartSync(Func<(ulong, WriteLogEvent), Task> syncHandler)
        {
            var lastPos = (ulong)(_lastPosForReplica(_targetReplicaId) + 1); // To adopt -1

            int itemsCount;
            do
            {
                (itemsCount, lastPos) = await SyncWriteLog(syncHandler, lastPos + 1);
            } while (itemsCount > 0);

            using (var callSubscribe = _syncService.Subscribe(new SyncSubscribeRequest { ReplicaId = _ownReplicaId }))
            {
                await SyncWriteLog(syncHandler, lastPos + 1);

                // NOTE: Don't change the order of Subscribe and SyncFrom as it may break "at-least-one" guarantee
                while (await callSubscribe.ResponseStream.MoveNext())
                {
                    var response = callSubscribe.ResponseStream.Current;
                    Console.WriteLine($"Sync Received: '{response}'");
                    await syncHandler((response.Pos, response.LogEvent));
                }
            }
        }

        private async Task<(int, ulong)> SyncWriteLog(Func<(ulong, WriteLogEvent), Task> syncHandler, ulong syncStartPos)
        {
            Console.WriteLine($"Started WriteLog Sync");
            ulong lastPos = syncStartPos;
            var itemsCount = 0;
            using (var callFrom = _syncService.SyncFrom(
                new SyncFromRequest { ReplicaId = _ownReplicaId, Since = syncStartPos, IncludeMine = false, IncludeAcked = false }))
            {
                while (await callFrom.ResponseStream.MoveNext())
                {
                    var response = callFrom.ResponseStream.Current;
                    switch (response.ResponseCase)
                    {
                        case SyncFromResponse.ResponseOneofCase.Item:
                            lastPos = response.Item.Pos;
                            Console.WriteLine($"Received: '{response}'");
                            await syncHandler((response.Item.Pos, response.Item.LogEvent));
                            itemsCount++;
                            break;
                        case SyncFromResponse.ResponseOneofCase.Footer:
                            lastPos = response.Footer.LastPos;
                            break;
                        case SyncFromResponse.ResponseOneofCase.None:
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            Console.WriteLine($"Synchronized '{itemsCount}' items");
            return (itemsCount, lastPos);
        }

        public void Dispose()
        {
            
        }
    }
}
