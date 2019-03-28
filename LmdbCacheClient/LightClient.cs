using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Grpc.Core;
using LmdbCache;
using LmdbCache.Domain;

namespace LmdbCacheClient
{
    public class LightClient : IClient, IDisposable
    {
        private readonly Channel _channel;
        private readonly LmdbCacheService.LmdbCacheServiceClient _lightClient;

        public LightClient(Channel channel)
        {
            _channel = channel;
            _lightClient = new LmdbCacheService.LmdbCacheServiceClient(channel);
        }

        private HashSet<string> TryAdd(IEnumerable<string> keys, Action<string, Stream> streamWriter, bool allowOverride,
            DateTimeOffset expiry, AvailabilityLevel requiredAvailabilityLevel)
        {
            // TODO: Add reactions to other AvailabilityLevels
            // TODO: Deal with expiration
            var batch = keys.Select(k => (k, new MemoryStream())).ToArray();

            foreach (var ks in batch)
            {
                streamWriter(ks.Item1, ks.Item2);
                ks.Item2.Position = 0;
            }

            var preparedBatch = batch.
                Select(ks => new AddRequest.Types.AddRequestEntry { Key = ks.Item1, Expiry = expiry.ToTimestamp(), Value = ByteString.FromStream(ks.Item2)}).
                ToArray();

            var request = new AddRequest {OverrideExisting = allowOverride};
            request.Entries.Add(preparedBatch);
            var addResponse = _lightClient.Add(request);

            return addResponse.Results.
                    Select((r, i) => (r, i)).
                    Where(res => res.Item1 == AddResponse.Types.AddResult.KeyAdded).
                    Select(res => preparedBatch[res.Item2].Key).ToHashSet(); // TODO: Reconfirm if successful or failed keys should be returned
        }

        public HashSet<string> TryAdd(IEnumerable<string> keys, Action<string, Stream> streamWriter, DateTimeOffset expiry,
            AvailabilityLevel requiredAvailabilityLevel = AvailabilityLevel.SavedToDisk) => TryAdd(keys, streamWriter, false, expiry, requiredAvailabilityLevel);

        public HashSet<string> TryAddOrUpdate(IEnumerable<string> keys, Action<string, Stream> streamWriter, DateTimeOffset expiry,
            AvailabilityLevel requiredAvailabilityLevel = AvailabilityLevel.SavedToDisk) => TryAdd(keys, streamWriter, true, expiry, requiredAvailabilityLevel);

        public HashSet<string> TryGet(IEnumerable<string> keys, Action<string, Stream> streamReader)
        {
            var keysCopied = keys.ToArray();

            var request = new GetRequest();
            request.Keys.AddRange(keysCopied);
            var getResponse = _lightClient.Get(request);

            var ret = getResponse.Results.Select((r, i) => (r, i)).Where(res => res.Item1.Result == GetResponse.Types.GetResponseEntry.Types.GetResult.Success).ToArray();

            foreach (var kv in ret)
            {
                streamReader(keysCopied[kv.Item2], kv.Item1.Value.SelectMany(v => v).ToStream());
            }

            return ret.Select(kv => keysCopied[kv.Item2]).ToHashSet();
        }

        public void TryCopy(IEnumerable<KeyValuePair<string, string>> keyCopies, DateTimeOffset expiry, out HashSet<string> fromKeysWhichDidNotExist,
            out HashSet<string> toKeysWhichAlreadyExisted, AvailabilityLevel requiredAvailabilityLevel = AvailabilityLevel.SavedToDisk)
        {
            var keysCopied = keyCopies.ToArray();

            var request = new CopyRequest();
            request.Entries.AddRange(keysCopied.Select(kv => new CopyRequest.Types.CopyRequestEntry{ Expiry = expiry.ToTimestamp()}));
            var copyResponse = _lightClient.Copy(request);

            var ret = copyResponse.Results.
                Select((r, i) => (r, i)).
                Aggregate((new List<string>(), new List<string>()), (acc, res) =>
                {
                    switch (res.Item1)
                    {
                        case CopyResponse.Types.CopyResult.Success:
                            break;
                        case CopyResponse.Types.CopyResult.FromKeyNotFound:
                            acc.Item1.Add(keysCopied[res.Item2].Key);
                            break;
                        case CopyResponse.Types.CopyResult.ToKeyExists:
                            acc.Item2.Add(keysCopied[res.Item2].Value);
                            break;
                        case CopyResponse.Types.CopyResult.Failure: // TODO: What to do in a case of failures?
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    return acc;
                });

            fromKeysWhichDidNotExist = ret.Item1.ToHashSet();
            toKeysWhichAlreadyExisted = ret.Item2.ToHashSet();
        }

        public HashSet<string> Contains(IEnumerable<string> keys)
        {
            var keysCopied = keys.ToArray();

            var request = new GetRequest();
            request.Keys.AddRange(keysCopied);
            var containsKeys = _lightClient.ContainsKeys(request);

            return containsKeys.Results.Select((r, i) => (r, i)).Where(res => res.Item1).Select(res => keysCopied[res.Item2]).ToHashSet();
        }

        public Dictionary<string, Dictionary<string, DiskUsageInfo>> GetDiskUsage(string server = null, int topNKeys = 0, string chunkStore = null) =>
            new Dictionary<string, Dictionary<string, DiskUsageInfo>>
            {
                { server, new Dictionary<string, DiskUsageInfo> { { server, new DiskUsageInfo { Usage = 1024 } } } } // TODO: Read from stats
            };

        public HashSet<string> TryDelete(IEnumerable<string> keys)
        {
            var keysCopied = keys.ToArray();

            var request = new DeleteRequest();
            request.Keys.AddRange(keysCopied);
            var containsKeys = _lightClient.Delete(request);

            return containsKeys.Results.Select((r, i) => (r, i)).Where(res => res.Item1 == DeleteResponse.Types.DeleteResult.Success).Select(res => keysCopied[res.Item2]).ToHashSet();
        }

        private async Task<IList<string>> SearchByPrefixAsync(string keyPrefix)
        {
            var ret = new List<string>();
            using (var reply = _lightClient.ListKeys(new KeyListRequest { KeyPrefix = keyPrefix }))
            {
                var responseStream = reply.ResponseStream;
                while (await responseStream.MoveNext())
                {
                    var response = responseStream.Current;
                    ret.Add(response.Key);
                }
            }

            return ret;
        }

        public HashSet<string> SearchByPrefix(string keyPrefix, int? depthIndex = null, string delimiter = null)
        {
            // TODO: Deal with depth and delimiter
            // TODO: Rethink pagination

            return SearchByPrefixAsync(keyPrefix).Result.ToHashSet();
        }

        public IDictionary<string, HashSet<string>> SearchManyByPrefix(IEnumerable<string> keysPrefixes, int? depthIndex = null, string delimiter = null)
        {
            var tasks = keysPrefixes.Select(keyPrefix => (keyPrefix, Task.Run(() => SearchByPrefixAsync(keyPrefix)))).ToArray();
            Task.WaitAll(tasks.Select(kv => kv.Item2 as Task).ToArray());

            return tasks.ToDictionary(t => t.Item1, t => t.Item2.Result.ToHashSet());
        }

        public void Dispose()
        {
            _channel.ShutdownAsync().Wait();
        } //=> _lightClient.Dispose();
    }
}
