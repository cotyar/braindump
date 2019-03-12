using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;

using LmdbCache;

namespace LmdbCacheServer
{
    class Program
    {
        const int Port = 50051;

        static void Main(string[] args)
        {
            Server server = new Server
            {
                //Services = { LmdbCacheService.BindService(new InMemoryCacheServiceImpl()) },
                Services = { LmdbCacheService.BindService(new LmdbCacheServiceImpl()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine("Greeter server listening on port " + Port);
            Console.WriteLine("Press any key to stop the server...");

            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}
