using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using LmdbCache;
using LmdbCacheServer.Tables;
using LmdbCacheWeb;
using LmdbLight;
using static LmdbCache.KvMetadata.Types;
using static LmdbCache.KvMetadata.Types.Status;
using static LmdbCache.KvMetadata.Types.UpdateAction;

namespace LmdbCacheServer.Replica
{
    public class Replica : IDisposable
    {
        private volatile VectorClock _clock;

        public ReplicaConfig ReplicaConfig { get; }
        public LightningConfig LightningConfig { get; }

        public VectorClock CurrentClock() => _clock.SetTimeNow();

        private readonly LightningPersistence _lmdb;
        private readonly KvMetadataTable _kvMetadataTable;
        private readonly ExpiryTable _kvExpiryTable;
        private readonly KvTable _kvTable;
        private readonly WriteLogTable _wlTable;
        private readonly Server _server;
        private readonly Server _serverReplication;
        private readonly Task _webServerCompletion;
        private readonly CancellationTokenSource _shutdownCancellationTokenSource;

        private readonly Replicator _replicator;
        private Task _syncProcessTask;
        private readonly string _ownHost;
        private readonly int _webUiPort;
        private readonly int _replicationPort;

        public Replica(ReplicaConfig replicaConfig, VectorClock clock = null)
        {
            _shutdownCancellationTokenSource = new CancellationTokenSource();

            ReplicaConfig = replicaConfig;
            _ownHost = ReplicaConfig.HostName ?? "127.0.0.1";
            _webUiPort = ReplicaConfig.WebUIPort ?? ReplicaConfig.Port + 1000;
            _replicationPort = ReplicaConfig.ReplicationPort ?? ReplicaConfig.Port + 2000;

            _clock = clock ?? VectorClockHelper.Create(ReplicaConfig.ReplicaId, 0);
            LightningConfig = replicaConfig.LightningConfig;

            _lmdb = new LightningPersistence(LightningConfig);
            _kvMetadataTable = new KvMetadataTable(_lmdb, "kvmetadata");
            _kvExpiryTable = new ExpiryTable(_lmdb, "kvexpiry");
            _wlTable = new WriteLogTable(_lmdb, "writelog", ReplicaConfig.ReplicaId);

            _kvTable = new KvTable(_lmdb, "kv", _kvExpiryTable, _kvMetadataTable,
                CurrentClock, (txn, wle) =>
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
                            case WriteLogEvent.LoggedEventOneofCase.Updated:
                                var addedOrUpdated = syncEvent.LogEvent.Updated;
                                var addMetadata = new KvMetadata
                                {
                                    Status = Active,
                                    Expiry = addedOrUpdated.Expiry,
                                    Action = Replicated,
                                    Updated = IncrementClock(),
                                    Compression = Compression.None
                                };
                                var wasUpdated = await _kvTable.AddOrUpdate(new KvKey(addedOrUpdated.Key),
                                    addMetadata,
                                    new KvValue(addedOrUpdated.Value));
                                // TODO: Should we do anything if the value wasn't updated? Maybe logging?                                
                                break;
                            case WriteLogEvent.LoggedEventOneofCase.Deleted:
                                var deleted = syncEvent.LogEvent.Deleted;
                                var currentClock = IncrementClock();
                                var delMetadata = new KvMetadata
                                {
                                    Status = Deleted,
                                    Expiry = currentClock.TicksOffsetUtc.ToTimestamp(),
                                    Action = Replicated,
                                    Updated = currentClock
                                };
                                var wasDeleted = await _kvTable.Delete(new KvKey(deleted.Key), delMetadata);
                                // TODO: Should we do anything if the value wasn't updated? Maybe logging?
                                break;
                            case WriteLogEvent.LoggedEventOneofCase.None:
                                throw new ArgumentException("syncEvent", $"Unexpected LogEvent case: {syncEvent.LogEvent.LoggedEventCase}");
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }); 
            }

            _webServerCompletion = WebServer.StartWebServer(_shutdownCancellationTokenSource.Token, _ownHost, _webUiPort);

            _serverReplication = new Server
            {
                Services = { SyncService.BindService(new ReplicatorMaster(_lmdb, replicaConfig.ReplicaId, _wlTable, replicaConfig.ReplicationPageSize ?? 1000u)) },
                Ports = { new ServerPort(_ownHost, _replicationPort, ServerCredentials.Insecure) }
            };
            _serverReplication.Start();

            Console.WriteLine("Replication server started listening on port " + _replicationPort);

            _server = new Server
            {
                //Services = { LmdbCacheService.BindService(new InMemoryCacheServiceImpl()) },
                Services = { LmdbCacheService.BindService(new LmdbCacheServiceImpl(_kvTable, CurrentClock)) },
                Ports = { new ServerPort(_ownHost, ReplicaConfig.Port, ServerCredentials.Insecure) }
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
