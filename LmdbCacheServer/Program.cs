using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;

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



        //static void Main(string[] args)
        //{
        //    var server = new Server
        //    {
        //        Services = { LmdbCacheService.BindService(new InMemoryCacheServiceImpl()) },
        //        //Services = { LmdbCacheService.BindService(new LmdbCacheServiceImpl()) },
        //        Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
        //    };
        //    server.Start();

        //    Console.WriteLine("Cache server listening on port " + Port);
        //    Console.WriteLine("Press any key to stop the server...");

        //    Console.ReadKey();

        //    server.ShutdownAsync().Wait();
        //}
    }
}
