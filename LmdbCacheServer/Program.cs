using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Logging;
using LmdbCache;
using LmdbCacheServer.Replica;
using LmdbLight;

namespace LmdbCacheServer
{
    class Program
    {
        private const int Port = 40051;

        static void Main(string[] args)
        {
            ConfigureLog($"logs/CacheServer_{DateTime.Now}.nlog");
            GC.AddMemoryPressure(2L * 1024 * 1024 * 1024);
            GrpcEnvironment.SetLogger(new LogLevelFilterLogger(new ConsoleLogger(), LogLevel.Debug));
            // GrpcEnvironment.SetLogger(new TextWriterLogger(new StreamWriter(File.OpenWrite($"logs/CacheServer_{DateTime.Now}.log"))));

            var lightningConfig = new LightningConfig
            {
                Name = @"C:\Work2\braindump\LmdbCacheServer\TestDir",
                MaxTables = 20,
                StorageLimit = 10,
                WriteBatchMaxDelegates = 1000,
                WriteBatchTimeoutMilliseconds = 1,
                SyncMode = LightningDbSyncMode.NoSync
            }; 

            var replicaConfigMaster = new ReplicaConfig
            {
                ReplicaId = "replica_1",
                Port = Port,
                MonitoringInterval = 10000,
                Replication = new ReplicationConfig { Port = Port + 2000, PageSize = 10000, UseBatching = false },
                Persistence = lightningConfig
            };

            var lightningConfigSlave = lightningConfig.Clone();
            lightningConfigSlave.Name = @"C:\Work2\braindump\LmdbCacheServer\TestDir_Slave";
            lightningConfigSlave.WriteBatchMaxDelegates = 10000;
            lightningConfigSlave.WriteBatchTimeoutMilliseconds = 1;
            lightningConfigSlave.SyncMode = LightningDbSyncMode.NoSync;
            var replicaConfigSlave = new ReplicaConfig
            {
                ReplicaId = "replica_2",
                MasterNode = $"{"127.0.0.1"}:{replicaConfigMaster.Replication.Port}",
                Replication = new ReplicationConfig { Port = Port + 2500, PageSize = 10000, UseBatching = false },
                Port = Port + 500,
                MonitoringInterval = 10000,
                Persistence = lightningConfigSlave
            };

            using (var server = new Replica.Replica(args.Length == 0 ? replicaConfigMaster : replicaConfigSlave))
            {

                Console.WriteLine("Cache server started on port " + Port);
                Console.WriteLine("Press any key to stop the server...");

                Console.ReadKey();
            }
        }

        private static void ConfigureLog(string logfileName)
        {
            var config = new NLog.Config.LoggingConfiguration();

            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = logfileName };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);
            config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, logfile);

            NLog.LogManager.Configuration = config;
        }
    }
}
