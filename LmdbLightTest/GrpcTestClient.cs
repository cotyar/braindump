using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Grpc.Core;
using LmdbCache;
using LmdbCache.Domain;
using LmdbCacheClient;
using LmdbCacheServer;
using LmdbCacheServer.Tables;
using LmdbLight;

namespace LmdbLightTest
{
    public class GrpcTestClient : IClient
    {
        private static int _startingPort = 55000;
        private readonly IClient _client;
        private readonly Server _server;

        public GrpcTestClient(LightningConfig config)
        {
            var port = Interlocked.Increment(ref _startingPort);
            config.Name = $"{config.Name ?? "db"}_{port}"; // It is safe to do as LightningConfig is a struct

            if (Directory.Exists(config.Name))
            {
                Directory.Delete(config.Name, true);
            }

            _server = new Server
            {
                //Services = { LmdbCacheService.BindService(new InMemoryCacheServiceImpl()) },
                Services = { LmdbCacheService.BindService(
                    new LmdbCacheServiceImpl(config, () => new VectorClock { TicksOffsetUtc = (ulong)DateTimeOffset.UtcNow.Ticks}, 
//                        (transaction, key, metadata, value) =>
//                        {
//                            Console.WriteLine($"Added key: '{key.Key}'");
//                        }
                        ((txn, wle) => Console.WriteLine($"Key changed: '{wle.LoggedEventCase} - {wle.Clock}'"))
                        )) },
                Ports = { new ServerPort("127.0.0.1", port, ServerCredentials.Insecure) }
            };
            _server.Start();

            Console.WriteLine("Cache server listening on port " + port);
            Console.WriteLine("Press any key to stop the server...");

            _client = new LightClient(new Channel($"127.0.0.1:{port}", ChannelCredentials.Insecure));
        }

        public HashSet<string> Contains(IEnumerable<string> keys) => _client.Contains(keys);

        public void Dispose()
        {
            _client.Dispose();
            _server.ShutdownAsync().Wait();
        }

        public Dictionary<string, Dictionary<string, DiskUsageInfo>> GetDiskUsage(string server = null, int topNKeys = 0, string chunkStore = null) => 
            _client.GetDiskUsage(server, topNKeys, chunkStore);

        public HashSet<string> SearchByPrefix(string keyPrefix, int? depthIndex = null, string delimiter = null) =>
            _client.SearchByPrefix(keyPrefix, depthIndex, delimiter);

        public IDictionary<string, HashSet<string>> SearchManyByPrefix(IEnumerable<string> keysPrefixes, int? depthIndex = null, string delimiter = null) =>
            _client.SearchManyByPrefix(keysPrefixes, depthIndex, delimiter);

        public HashSet<string> TryAdd(IEnumerable<string> keys, Action<string, Stream> streamWriter, DateTimeOffset expiry, AvailabilityLevel requiredAvailabilityLevel = AvailabilityLevel.SavedToDisk) =>
            _client.TryAdd(keys, streamWriter, expiry, requiredAvailabilityLevel);

        public HashSet<string> TryAddOrUpdate(IEnumerable<string> keys, Action<string, Stream> streamWriter, DateTimeOffset expiry, AvailabilityLevel requiredAvailabilityLevel = AvailabilityLevel.SavedToDisk) =>
            _client.TryAddOrUpdate(keys, streamWriter, expiry, requiredAvailabilityLevel);

        public void TryCopy(IEnumerable<KeyValuePair<string, string>> keyCopies, DateTimeOffset expiry, out HashSet<string> fromKeysWhichDidNotExist, out HashSet<string> toKeysWhichAlreadyExisted, AvailabilityLevel requiredAvailabilityLevel = AvailabilityLevel.SavedToDisk) =>
            _client.TryCopy(keyCopies, expiry, out fromKeysWhichDidNotExist, out toKeysWhichAlreadyExisted, requiredAvailabilityLevel);

        public HashSet<string> TryDelete(IEnumerable<string> keys) => _client.TryDelete(keys);

        public HashSet<string> TryGet(IEnumerable<string> keys, Action<string, Stream> streamReader) => _client.TryGet(keys, streamReader);
    }
}
