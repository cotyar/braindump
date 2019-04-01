using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using LmdbCache;
using LmdbCacheServer.Tables;
using LmdbLight;

namespace LmdbCacheServer.Replica
{
    public class Replica : IDisposable
    {
        private volatile VectorClock _clock;

        public ReplicaConfig ReplicaConfig { get; }
        public LightningConfig LightningConfig { get; }

        public VectorClock CurrentClock() => _clock.SetTimeNow();

        private readonly LightningPersistence _lmdb;
        private readonly ExpiryTable _kvExpiryTable;
        private readonly KvTable _kvTable;
        private readonly WriteLogTable _wlTable;
        private readonly Server _server;
        private readonly Server _serverReplication;

        private readonly Replicator _replicator;
        private Task _syncProcessTask;

        public Replica(ReplicaConfig replicaConfig, VectorClock clock = null)
        {
            ReplicaConfig = replicaConfig;
            _clock = clock ?? VectorClockHelper.Create(ReplicaConfig.ReplicaId, 0);
            LightningConfig = replicaConfig.LightningConfig;

            _lmdb = new LightningPersistence(LightningConfig);
            _kvExpiryTable = new ExpiryTable(_lmdb, "kvexpiry");
            _wlTable = new WriteLogTable(_lmdb, "writelog", ReplicaConfig.ReplicaId);

            _kvTable = new KvTable(_lmdb, "kv", _kvExpiryTable,
                CurrentClock, (transaction, table, key, expiry) => { }, (txn, wle) =>
                {
                    wle.Clock = IncrementClock();

                    if (!_wlTable.AddLogEvents(txn, wle))
                    {
                        throw new EventLogException($"Cannot write event to the WriteLog: '{wle}'"); // TODO: Values can be large, possibly exclude them from the Exception.
                    }
                });

            if (!string.IsNullOrWhiteSpace(ReplicaConfig.MasterNode))
            {
                // TODO: Add supervision

                _replicator = new Replicator(ReplicaConfig.ReplicaId, new Channel(ReplicaConfig.MasterNode, ChannelCredentials.Insecure), s => VectorClockHelper.CreateEmpty());
                _syncProcessTask = _replicator.StartSync(async syncEvent =>
                    {
                        switch (syncEvent.LogEvent.LoggedEventCase)
                        {
                            case WriteLogEvent.LoggedEventOneofCase.None:
                                throw new ArgumentException("syncEvent", $"Unexpected LogEvent case: {syncEvent.LogEvent.LoggedEventCase}");
                            case WriteLogEvent.LoggedEventOneofCase.Updated:
                                var addedOrUpdated = syncEvent.LogEvent.Updated;
                                var wasUpdated = await _kvTable.AddOrUpdate(new KvKey(addedOrUpdated.Key),
                                    new KvMetadata(addedOrUpdated.Expiry, syncEvent.LogEvent.Clock),
                                    new KvValue(new[] {addedOrUpdated.Value}));
                                // TODO: Should we do anything if the value wasn't updated? Maybe logging?                                
                                break;
                            case WriteLogEvent.LoggedEventOneofCase.Deleted:
                                var deleted = syncEvent.LogEvent.Deleted;
                                var wasDeleted = await _kvTable.Delete(new KvKey(deleted.Key));
                                // TODO: Should we do anything if the value wasn't updated? Maybe logging?
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }); 
            }

            _serverReplication = new Server
            {
                Services = { SyncService.BindService(new ReplicatorMaster(_lmdb, replicaConfig.ReplicaId, _wlTable, replicaConfig.ReplicationPageSize ?? 1000u)) },
                Ports = { new ServerPort(ReplicaConfig.HostName ?? "127.0.0.1", ReplicaConfig.ReplicationPort, ServerCredentials.Insecure) }
            };
            _serverReplication.Start();

            Console.WriteLine("Replication server started listening on port " + ReplicaConfig.Port);

            _server = new Server
            {
                //Services = { LmdbCacheService.BindService(new InMemoryCacheServiceImpl()) },
                Services = { LmdbCacheService.BindService(new LmdbCacheServiceImpl(_kvTable, CurrentClock)) },
                Ports = { new ServerPort(ReplicaConfig.HostName ?? "127.0.0.1", ReplicaConfig.Port, ServerCredentials.Insecure) }
            };
            _server.Start();

            Console.WriteLine("Cache server listening on port " + ReplicaConfig.Port);
        }

        private VectorClock IncrementClock() => _clock = _clock.Increment(ReplicaConfig.ReplicaId);

        public void Dispose()
        {
            // Dispose other members and implement Dispose pattern properly
            //_server.ShutdownAsync().Wait();
            //_serverReplication.ShutdownAsync().Wait();
            _lmdb.Dispose(); 
        }
    }
}
