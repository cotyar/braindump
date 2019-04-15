using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Utils;
using LmdbCache;
using LmdbCacheServer.Tables;
using LmdbLight;
using static LmdbCache.KvMetadata.Types;
using static LmdbCache.KvMetadata.Types.Status;
using static LmdbCache.KvMetadata.Types.UpdateAction;
using static LmdbCache.WriteLogEvent.Types;

namespace LmdbCacheServer.Replica
{
    public class ReplicatorSlave : IDisposable
    {
        private readonly LightningPersistence _lmdb;
        private readonly string _ownReplicaId;
        private readonly KvTable _kvTable;
        private readonly ReplicationTable _replicationTable;
        private readonly Func<WriteTransaction, VectorClock> _incrementClock;
        private readonly string _targetReplicaId;
        private readonly SyncService.SyncServiceClient _syncService;

        public ReplicatorSlave(LightningPersistence lmdb, string ownReplicaId, Channel syncChannel, 
            KvTable kvTable, ReplicationTable replicationTable,
            Func<WriteTransaction, VectorClock> incrementClock) // TODO: Add ACKs streaming
        {
            _lmdb = lmdb;
            _ownReplicaId = ownReplicaId;
            _kvTable = kvTable;
            _replicationTable = replicationTable;
            _incrementClock = incrementClock;

            _syncService = new SyncService.SyncServiceClient(syncChannel);
            _targetReplicaId = _syncService.GetReplicaId(new Empty()).ReplicaId; // TODO: Add timeouts
        }

        public async Task StartSync()
        {
            var lastPos = _replicationTable.GetLastPos(_targetReplicaId) ?? 0 + 1; 

            int itemsCount;
            do
            {
                (itemsCount, lastPos) = await SyncWriteLog(lastPos);
                if (itemsCount > 0) lastPos++; // avoid repeating the last item
            } while (itemsCount > 0);

            using (var callSubscribe = _syncService.Subscribe(new SyncSubscribeRequest { ReplicaId = _ownReplicaId }))
            {
                await SyncWriteLog(lastPos);

                // NOTE: Don't change the order of Subscribe and SyncFrom as it may break "at-least-one" guarantee
                while (await callSubscribe.ResponseStream.MoveNext())
                {
                    var response = callSubscribe.ResponseStream.Current;
                    Console.WriteLine($"Sync Received: '{response}'");
                    await SyncHandler((response.Pos, response.LogEvent));
                }
            }
        }

        private async Task<(int, ulong)> SyncWriteLog(ulong syncStartPos)
        {
            Console.WriteLine($"Started WriteLog Sync");
            ulong lastPos = syncStartPos;
            var itemsCount = 0;
            using (var callFrom = _syncService.SyncFrom(
                new SyncFromRequest { ReplicaId = _ownReplicaId, Since = syncStartPos, IncludeMine = false, IncludeAcked = false }))
            {
                await callFrom.ResponseStream.ForEachAsync(async response =>
                {
                    Console.WriteLine($"Response received");
                    switch (response.ResponseCase)
                    {
                        case SyncFromResponse.ResponseOneofCase.Items:
//                            Console.WriteLine($"Received: '{response}'");
                            foreach (var responseItem in response.Items.Batch)
                            {
                                await SyncHandler((responseItem.Pos, responseItem.LogEvent));
                                itemsCount++;
                            }
                            lastPos = response.Items.Batch.Last().Pos;
                            break;
                        case SyncFromResponse.ResponseOneofCase.Footer:
                            lastPos = response.Footer.LastPos;
                            break;
                        case SyncFromResponse.ResponseOneofCase.None:
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
            }
            Console.WriteLine($"Synchronized '{itemsCount}' items");
            return (itemsCount, lastPos);
        }

        private async Task SyncHandler((ulong, WriteLogEvent) syncEvent)
        {
            // TODO: Update last pos
            switch (syncEvent.Item2.LoggedEventCase)
            {
                case WriteLogEvent.LoggedEventOneofCase.Updated:
                    var addedOrUpdated = syncEvent.Item2.Updated;
                    await _lmdb.WriteAsync(txn =>
                    {
                        var addMetadata = new KvMetadata
                        {
                            Status = Active,
                            Expiry = addedOrUpdated.Expiry,
                            Action = Replicated,
                            Updated = _incrementClock(txn),
                            Compression = Compression.None // TODO: Use correct compression mode
                        };

                        var wasUpdated = _kvTable.Add(
                            txn,
                            new KvKey(addedOrUpdated.Key),
                            addMetadata,
                            new KvValue(addedOrUpdated.Value),
                            addMetadata.Updated,
                            (key, vcOld, vcNew) => vcOld.Earlier(vcNew));
                        _replicationTable.SetLastPos(txn, _targetReplicaId, syncEvent.Item1);
                        // TODO: Should we do anything if the value wasn't updated? Maybe logging?        
                    }, false);
                    break;
                case WriteLogEvent.LoggedEventOneofCase.Deleted:
                    var deleted = syncEvent.Item2.Deleted;
                    await _lmdb.WriteAsync(txn =>
                    {
                        var currentClock = _incrementClock(txn);
                        var delMetadata = new KvMetadata
                        {
                            Status = KvMetadata.Types.Status.Deleted,
                            Expiry = currentClock.TicksOffsetUtc.ToTimestamp(),
                            Action = Replicated,
                            Updated = currentClock
                        };

                        var kvKey = new KvKey(deleted.Key);
                        _kvTable.Delete(txn, kvKey, delMetadata);
                        _replicationTable.SetLastPos(txn, _targetReplicaId, syncEvent.Item1);
                        // TODO: Should we do anything if the value wasn't updated? Maybe logging?
                    }, false);
                    break;
                case WriteLogEvent.LoggedEventOneofCase.None:
                    throw new ArgumentException("syncEvent", $"Unexpected LogEvent case: {syncEvent.Item2.LoggedEventCase}");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Dispose()
        {
            
        }
    }
}
