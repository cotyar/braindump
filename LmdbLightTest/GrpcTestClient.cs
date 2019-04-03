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
using LmdbCacheServer.Replica;
using LmdbCacheServer.Tables;
using LmdbLight;

namespace LmdbLightTest
{
    public class GrpcTestClient : IClient
    {
        private static int _startingPort = 55000;
        private readonly IClient _client;
        private readonly Replica _server;

        public readonly ReplicaConfig ReplicaConfig;

        public GrpcTestClient(LightningConfig lightningConfig)
        {
            var port = Interlocked.Increment(ref _startingPort);
            lightningConfig.Name = $"{lightningConfig.Name ?? "db"}_{port}"; // It is safe to do as LightningConfig is a struct

            if (Directory.Exists(lightningConfig.Name))
            {
                Directory.Delete(lightningConfig.Name, true);
            }

            ReplicaConfig = new ReplicaConfig
            {
                ReplicaId = "replica_1",
                Port = port,
                ReplicationPort = port + 2000,
                LightningConfig = lightningConfig
            };
            _server = new Replica(ReplicaConfig);

            _client = new LightClient(new Channel($"127.0.0.1:{port}", ChannelCredentials.Insecure), true);
        }

        public HashSet<string> Contains(IEnumerable<string> keys) => _client.Contains(keys);

        public void Dispose()
        {
            _client.Dispose();
            _server.Dispose();
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
