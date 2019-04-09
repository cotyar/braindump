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
            LightningConfig lightningConfig = new LightningConfig
            {
                Name = "TestDir",
                MaxTables = 20,
                StorageLimit = 10,
                WriteBatchMaxDelegates = 100,
                WriteBatchTimeoutMilliseconds = 0,
                SyncMode = LightningDbSyncMode.Fsync
            }; 

            var replicaConfig = new ReplicaConfig
            {
                ReplicaId = "replica_1",
                Port = Port,
                ReplicationPort = Port + 2000,
                LightningConfig = lightningConfig
            };

            using (var server = new Replica.Replica(replicaConfig))
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
