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
using Monitor = LmdbCacheServer.Monitoring.Monitor;

namespace LmdbCacheServer.Replica
{
    public class Replica : IDisposable
    {
        //private volatile VectorClock _clock;

        public ReplicaConfig ReplicaConfig { get; }
        public LightningConfig LightningConfig { get; }

        public VectorClock CurrentClock() => _lmdb.Read(txn => _statusTable.GetLastClock(txn)).SetTimeNow();

        private readonly LightningPersistence _lmdb;

        private readonly KvMetadataTable _kvMetadataTable;
        private readonly ExpiryTable _kvExpiryTable;
        private readonly ReplicaStatusTable _statusTable;
        private readonly ReplicationTable _replicationTable;
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

            LightningConfig = ReplicaConfig.Persistence;

            _lmdb = new LightningPersistence(LightningConfig);
            _kvMetadataTable = new KvMetadataTable(_lmdb, "kvmetadata");
            _kvExpiryTable = new ExpiryTable(_lmdb, "kvexpiry");
            _statusTable = new ReplicaStatusTable(_lmdb, "replicastatus", ReplicaConfig.ReplicaId);
            _replicationTable = new ReplicationTable(_lmdb, "replication");
            _wlTable = new WriteLogTable(_lmdb, "writelog", ReplicaConfig.ReplicaId);

            _kvTable = new KvTable(_lmdb, "kv", _statusTable, _kvExpiryTable, _kvMetadataTable,
                CurrentClock, IncrementClock, (txn, wle) =>
                {
                    wle.Clock = IncrementClock(txn);

                    if (!_wlTable.AddLogEvents(txn, wle))
                    {
                        throw new EventLogException($"Cannot write event to the WriteLog: '{wle}'"); // TODO: Values can be large, possibly exclude them from the Exception.
                    }
                });

            if (!string.IsNullOrWhiteSpace(ReplicaConfig.MasterNode))
            {
                // TODO: Add supervision

                _replicatorSlave = new ReplicatorSlave(_lmdb, ReplicaConfig.ReplicaId, new Channel(ReplicaConfig.MasterNode, ChannelCredentials.Insecure),
                    _kvTable, _replicationTable, _wlTable, ReplicaConfig.Replication, IncrementClockWithRemoteUpdate);
                _syncProcessTask = GrpcSafeHandler(() => _replicatorSlave.StartSync()); 
            }

            _webServerCompletion = WebServer.StartWebServer(_shutdownCancellationTokenSource.Token, ReplicaConfig.HostName, ReplicaConfig.WebUIPort);

            _monitor = new Monitor(CollectStats, ReplicaConfig.MonitoringInterval);
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
                    _kvTable, _replicationTable, _wlTable, ReplicaConfig.Replication, IncrementClockWithRemoteUpdate)) },
                Ports = { new ServerPort(ReplicaConfig.HostName, (int)ReplicaConfig.Replication.Port, ServerCredentials.Insecure) }
            };
            _serverReplication.Start();

            Console.WriteLine("Replication server started listening on port " + ReplicaConfig.Replication.Port);

            _server = new Server
            {
                //Services = { LmdbCacheService.BindService(new InMemoryCacheServiceImpl()) },
                Services = { LmdbCacheService.BindService(new LmdbCacheServiceImpl(_kvTable)) },
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

            if (config.Replication.Port == 0) config.Replication.Port = config.Port + 2000;
            if (config.Replication.PageSize == 0) config.Replication.PageSize = 1000u;

            if (config.MonitoringPort == 0) config.MonitoringPort = config.Port + 3000;
            if (config.MonitoringInterval == 0) config.MonitoringInterval = 10000u;

            return config;
        }

        private VectorClock IncrementClock(WriteTransaction txn)
        {
            var clock = _statusTable.GetLastClock(txn).Increment(ReplicaConfig.ReplicaId);
            _statusTable.SetLastClock(txn, clock);
            return clock;
        }

        private VectorClock IncrementClockWithRemoteUpdate(WriteTransaction txn, string remoteReplica, ulong remotePos)
        {
            var clock = _statusTable.GetLastClock(txn).Increment(ReplicaConfig.ReplicaId);
            var oldRemotePos = clock.GetReplicaValue(remoteReplica);
            if (oldRemotePos < remotePos)
            {
                clock = clock.SetReplicaValue(remoteReplica, remotePos);
            }
            _statusTable.SetLastClock(txn, clock);
            return clock;
        }

        private Task<ReplicaStatus> CollectStats()
        {
            var (counters, collectedStats) = _lmdb.Read(txn =>
            {
                var stats = new CollectedStats();
                foreach (var (key, metadata) in _kvTable.MetadataByPrefix(txn, new KvKey(""), 0, uint.MaxValue))
                {
                    switch (metadata.Status)
                    {
                        case KvMetadata.Types.Status.Active:
                            stats.ActiveKeys++;
                            stats.NonExpiredKeys++;
                            break;
                        case KvMetadata.Types.Status.Deleted:
                            stats.DeletedKeys++;
                            stats.NonExpiredKeys++;
                            break;
                        case KvMetadata.Types.Status.Expired:
                            stats.ExpiredKeys++;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    stats.AllKeys++;
                }

                return (_statusTable.GetCounters(txn), stats);
            });

            var status = new ReplicaStatus
            {
                ReplicaId = ReplicaConfig.ReplicaId,
                ConnectionInfo = new ReplicaConnectionInfo(),
                Started = _started,
                ReplicaConfig = ReplicaConfig,
                CurrentClock = CurrentClock(),

                Counters = counters,
                CollectedStats = collectedStats,
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
