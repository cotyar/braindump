using Grpc.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Google.Protobuf;
using Grpc.Core.Utils;
using LmdbCache;

namespace LmdbCacheClient
{
    class Program
    {
        static async Task StartServer()
        {
            var channel = new Channel("127.0.0.1:40051", ChannelCredentials.Insecure);
            var client = new LmdbCacheService.LmdbCacheServiceClient(channel);

            var monitoringChannel = new Channel("127.0.0.1:43051", ChannelCredentials.Insecure);
            var monitoringClient = new MonitoringService.MonitoringServiceClient(monitoringChannel);

            Console.WriteLine($"Status: {monitoringClient.GetStatus(new MonitoringUpdateRequest { CorrelationId = "Client monitor 1" })}");
            var monitor = monitoringClient.Subscribe(new MonitoringUpdateRequest {CorrelationId = "Client monitor"});

            var cancellationTokenSource = new CancellationTokenSource();

            var t = Task.Run(async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested 
                       && await monitor.ResponseStream.MoveNext(cancellationTokenSource.Token))
                {
                    Console.WriteLine($"Update: {monitor.ResponseStream.Current}");
                }
            });

            Console.WriteLine("Connected to the server");
            Console.WriteLine("Press Enter to stop ...");

            Console.ReadLine();
            cancellationTokenSource.Cancel();

            await monitoringChannel.ShutdownAsync();
            await channel.ShutdownAsync();
        }

        static void Main(string[] args)
        {
            StartServer().Wait();
        }

        private static readonly Random Rnd = new Random();

        private static byte[] RandomArray(int length)
        {
            var buffer = new byte[length];
            Rnd.NextBytes(buffer);
            return buffer;
        }

        private static ByteString RandomByteString(int length) => ByteString.CopyFrom(RandomArray(length));

        private static Stream RandomByteStream(int length) => new MemoryStream(RandomArray(length));
    }
}
