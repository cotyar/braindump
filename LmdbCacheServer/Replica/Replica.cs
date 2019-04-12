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
using static LmdbCache.Helper;
using static LmdbCache.KvMetadata.Types;
using static LmdbCache.KvMetadata.Types.Status;
using static LmdbCache.KvMetadata.Types.UpdateAction;
using Monitor = LmdbCacheServer.Monitoring.Monitor;

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
        private readonly ReplicaStatusTable _replicaStatusTable;
        private readonly KvTable _kvTable;
        private readonly WriteLogTable _wlTable;

        private readonly Server _server;
        private readonly Server _serverReplication;
        private readonly Server _serverMonitoring;
        private readonly Task _webServerCompletion;
        private readonly CancellationTokenSource _shutdownCancellationTokenSource;
        private readonly Timestamp _started;

        private readonly ReplicatorSlave _replicatorSlave;
        private Task _syncProcessTask;
        private readonly Monitor _monitor;

        public Replica(ReplicaConfig replicaConfig, VectorClock clock = null)
        {
            //GrpcEnvironment.SetCompletionQueueCount(1);

            _started = DateTimeOffset.UtcNow.ToTimestamp();
            _shutdownCancellationTokenSource = new CancellationTokenSource();

            ReplicaConfig = AdjustConfig(replicaConfig);

            _clock = clock ?? VectorClockHelper.Create(ReplicaConfig.ReplicaId, 0);
            LightningConfig = ReplicaConfig.LightningConfig;

            _lmdb = new LightningPersistence(LightningConfig);
            _kvMetadataTable = new KvMetadataTable(_lmdb, "kvmetadata");
            _kvExpiryTable = new ExpiryTable(_lmdb, "kvexpiry");
            _replicaStatusTable = new ReplicaStatusTable(_lmdb, "replicastatus", ReplicaConfig.ReplicaId);
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

                _replicatorSlave = new ReplicatorSlave(ReplicaConfig.ReplicaId, new Channel(ReplicaConfig.MasterNode, ChannelCredentials.Insecure), s => -1);
                _syncProcessTask = GrpcSafeHandler(() => _replicatorSlave.StartSync(async syncEvent =>
                    {
                        // TODO: Update last pos
                        switch (syncEvent.Item2.LoggedEventCase)
                        {
                            case WriteLogEvent.LoggedEventOneofCase.Updated:
                                var addedOrUpdated = syncEvent.Item2.Updated;
                                var addMetadata = new KvMetadata
                                {
                                    Status = Active,
                                    Expiry = addedOrUpdated.Expiry,
                                    Action = Replicated,
                                    Updated = IncrementClock(),
                                    Compression = Compression.None // TODO: Use correct compression mode
                                };
                                var wasUpdated = await _kvTable.AddOrUpdate(new KvKey(addedOrUpdated.Key),
                                    addMetadata,
                                    new KvValue(addedOrUpdated.Value));
                                // TODO: Should we do anything if the value wasn't updated? Maybe logging?                                
                                break;
                            case WriteLogEvent.LoggedEventOneofCase.Deleted:
                                var deleted = syncEvent.Item2.Deleted;
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
                                throw new ArgumentException("syncEvent", $"Unexpected LogEvent case: {syncEvent.Item2.LoggedEventCase}");
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    })); 
            }

            _webServerCompletion = WebServer.StartWebServer(_shutdownCancellationTokenSource.Token, ReplicaConfig.HostName, ReplicaConfig.WebUIPort);

            _monitor = new Monitor(CollectStats, 10000);
            _serverMonitoring = new Server
            {
                Services = { MonitoringService.BindService(_monitor) },
                Ports = { new ServerPort(ReplicaConfig.HostName, (int)ReplicaConfig.MonitoringPort, ServerCredentials.Insecure) }
            };
            _serverMonitoring.Start();

            Console.WriteLine("Monitoring server started listening on port " + ReplicaConfig.MonitoringPort);

            _serverReplication = new Server
            {
                Services = { SyncService.BindService(new ReplicatorMaster(_lmdb, replicaConfig.ReplicaId, 
                    (txn, key) => _kvTable.TryGet(txn, new KvKey(key)).Item2?.Value, _wlTable, ReplicaConfig.ReplicationPageSize)) },
                Ports = { new ServerPort(ReplicaConfig.HostName, (int)ReplicaConfig.ReplicationPort, ServerCredentials.Insecure) }
            };
            _serverReplication.Start();

            Console.WriteLine("Replication server started listening on port " + ReplicaConfig.ReplicationPort);

            _server = new Server
            {
                //Services = { LmdbCacheService.BindService(new InMemoryCacheServiceImpl()) },
                Services = { LmdbCacheService.BindService(new LmdbCacheServiceImpl(_kvTable, CurrentClock)) },
                Ports = { new ServerPort(ReplicaConfig.HostName, (int)ReplicaConfig.Port, ServerCredentials.Insecure) }
            };
            _server.Start();

            Console.WriteLine("Cache server listening on port " + ReplicaConfig.Port);
        }

        private static ReplicaConfig AdjustConfig(ReplicaConfig config)
        {
            config = config.Clone();

            if (string.IsNullOrWhiteSpace(config.HostName)) config.HostName = "127.0.0.1";
            if (config.WebUIPort == 0) config.WebUIPort = config.Port + 1000;
            if (config.ReplicationPort == 0) config.ReplicationPort = config.Port + 2000;
            if (config.MonitoringPort == 0) config.MonitoringPort = config.Port + 3000;
            if (config.ReplicationPageSize == 0) config.ReplicationPageSize = 1000u;

            return config;
        }

        private VectorClock IncrementClock() => _clock = _clock.Increment(ReplicaConfig.ReplicaId);

        private Task<ReplicaStatus> CollectStats()
        {
            var status = new ReplicaStatus
            {
                ReplicaId = ReplicaConfig.ReplicaId,
                ConnectionInfo = new ReplicaConnectionInfo(),
                Started = _started,
                ReplicaConfig = ReplicaConfig,
                CurrentClock = CurrentClock(),

                Counters = _lmdb.Read(txn => _replicaStatusTable.GetCounters(txn)),
                ClusterStatus = null
            };

            return Task.FromResult(status);
        }

        public void Dispose()
        {
            // Dispose other members and implement Dispose pattern properly
            //_server.ShutdownAsync().Wait();
            //_serverReplication.ShutdownAsync().Wait();
            _monitor.Dispose();
            _lmdb.Dispose(); 
        }
    }
}
