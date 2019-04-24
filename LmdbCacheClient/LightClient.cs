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
using static LmdbCache.AddRequest.Types;
using static LmdbCache.AddStreamRequest.Types;
using static LmdbCache.GetResponse.Types;
using static LmdbCache.KvMetadata.Types;
using static LmdbCache.ValueMetadata.Types;
using static LmdbCache.ValueMetadata.Types.Compression;
using static LmdbCache.ValueMetadata.Types.HashedWith;

namespace LmdbCacheClient
{
    public class LightClient : IClient
    {
        private readonly Channel _channel;
        private readonly ClientConfig _clientConfig;
        private readonly LmdbCacheService.LmdbCacheServiceClient _lightClient;
        private readonly string _correlationId;
        private readonly ValueHasher _valueHasher;
        private readonly ValueCompressor _valueCompressor;

        public LightClient(Channel channel, ClientConfig clientConfig)
        {
            _channel = channel;
            _clientConfig = clientConfig;
            _lightClient = new LmdbCacheService.LmdbCacheServiceClient(channel);
            _correlationId = Guid.NewGuid().ToString("N");
            _valueHasher = new ValueHasher();
            _valueCompressor = new ValueCompressor();
        }

        private async Task<AddResponse> AddStreaming(AddRequestEntry[] entries, bool allowOverride)
        {
            using (var call = _lightClient.AddStream())
            {
                var header = new AddStreamRequest { Header = new Header
                    {
                        OverrideExisting = allowOverride,
                        CorrelationId = _correlationId, 
                        ChunksCount = (uint)entries.Length
                    } };
                await call.RequestStream.WriteAsync(header);

                for (var i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    var chunk = new AddStreamRequest { Chunk = new DataChunk { Entry = entry, Index = (uint) i } };
                    await call.RequestStream.WriteAsync(chunk);
                }

                await call.RequestStream.CompleteAsync();

                return await call.ResponseAsync;
            }
        }

        private HashSet<string> TryAdd(IEnumerable<string> keys, Action<string, Stream> streamWriter, bool allowOverride,
            DateTimeOffset expiry, AvailabilityLevel requiredAvailabilityLevel)
        {
            // TODO: Add reactions to other AvailabilityLevels
            var batch = keys.Select(k => (k, new MemoryStream())).ToArray();

            foreach (var ks in batch)
            {
                streamWriter(ks.Item1, ks.Item2);
                ks.Item2.Position = 0;
            }

            var preparedBatch = batch.
                Select(ks =>
                {
                    var value = ByteString.FromStream(_valueCompressor.Compress(_clientConfig.Compression, ks.Item2));
                    return new AddRequestEntry
                    {
                        Key = ks.Item1,
                        Expiry = expiry.ToTimestamp(),
                        ValueMetadata = new ValueMetadata
                        {
                            Compression = _clientConfig.Compression,
                            HashedWith = _clientConfig.HashedWith,
                            Hash = ByteString.CopyFrom(_valueHasher.ComputeHash(_clientConfig.HashedWith, value.ToByteArray()))
                        },
                        Value = value
                    };
                }).
                ToArray();

            var addResponse = Task.Run(async () =>
                {
                    if (_clientConfig.UseStreaming)
                    {
                        return await AddStreaming(preparedBatch, allowOverride);
                    }

                    var request = new AddRequest
                    {
                        Header = new Header
                        {
                            OverrideExisting = allowOverride,
                            CorrelationId = _correlationId,
                            ChunksCount = (uint)preparedBatch.Length
                        }
                    };
                    request.Entries.Add(preparedBatch);
                    return await _lightClient.AddAsync(request);
                }).GetAwaiter().GetResult();

            return addResponse.Results.
                    Select((r, i) => (r, i)).
                    Where(res => res.Item1 == AddResponse.Types.AddResult.KeyAdded).
                    Select(res => preparedBatch[res.Item2].Key).ToHashSet(); // TODO: Reconfirm if successful or failed keys should be returned
        }

        public HashSet<string> TryAdd(IEnumerable<string> keys, Action<string, Stream> streamWriter, DateTimeOffset expiry,
            AvailabilityLevel requiredAvailabilityLevel = AvailabilityLevel.SavedToDisk) => TryAdd(keys, streamWriter, false, expiry, requiredAvailabilityLevel);

        public HashSet<string> TryAddOrUpdate(IEnumerable<string> keys, Action<string, Stream> streamWriter, DateTimeOffset expiry,
            AvailabilityLevel requiredAvailabilityLevel = AvailabilityLevel.SavedToDisk) => TryAdd(keys, streamWriter, true, expiry, requiredAvailabilityLevel);

        private async Task<IEnumerable<GetResponseEntry>> GetStream(GetRequest request)
        {
            var ret = new List<GetResponseEntry>(request.Keys.Count);
            using (var call = _lightClient.GetStream(request))
            {
                while (await call.ResponseStream.MoveNext())
                {
                    ret.Add(call.ResponseStream.Current.Result);
                }
            }

            return ret;
        }

        public HashSet<string> TryGet(IEnumerable<string> keys, Action<string, Stream> streamReader)
        {
            var keysCopied = keys.ToArray();

            var request = new GetRequest { CorrelationId = _correlationId };
            request.Keys.AddRange(keysCopied);
            var results = Task.Run(async () => _clientConfig.UseStreaming
                ? await GetStream(request)
                : (await _lightClient.GetAsync(request)).Results
            ).GetAwaiter().GetResult();

            var ret = results.Where(res => res.Result == GetResponseEntry.Types.GetResult.Success).ToArray();

            foreach (var kv in ret)
            {
                streamReader(keysCopied[kv.Index], kv.Value.ToStream());
            }

            return ret.Select(kv => keysCopied[kv.Index]).ToHashSet();
        }

        public void TryCopy(IEnumerable<KeyValuePair<string, string>> keyCopies, DateTimeOffset expiry, out HashSet<string> fromKeysWhichDidNotExist,
            out HashSet<string> toKeysWhichAlreadyExisted, AvailabilityLevel requiredAvailabilityLevel = AvailabilityLevel.SavedToDisk)
        {
            var keysCopied = keyCopies.ToArray();

            var request = new CopyRequest { CorrelationId = _correlationId };
            request.Entries.AddRange(keysCopied.Select(kv => new CopyRequest.Types.CopyRequestEntry{ Expiry = expiry.ToTimestamp()}));
            var copyResponse = Task.Run(async () => await _lightClient.CopyAsync(request)).GetAwaiter().GetResult();

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

            var request = new GetRequest { CorrelationId = _correlationId };
            request.Keys.AddRange(keysCopied);
            var containsKeys = Task.Run(async () => await _lightClient.ContainsKeysAsync(request)).GetAwaiter().GetResult();

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

            var request = new DeleteRequest { CorrelationId = _correlationId };
            request.Keys.AddRange(keysCopied);
            var containsKeys = Task.Run(async () => await _lightClient.DeleteAsync(request)).GetAwaiter().GetResult();

            return containsKeys.Results.Select((r, i) => (r, i)).Where(res => res.Item1 == DeleteResponse.Types.DeleteResult.Success).Select(res => keysCopied[res.Item2]).ToHashSet();
        }

        private async Task<IList<string>> SearchByPrefixAsync(string keyPrefix)
        {
            var ret = new List<string>();
            using (var reply = _lightClient.ListKeys(new KeyListRequest
            {
                KeyPrefix = keyPrefix,
                CorrelationId = _correlationId,
                Page = 0,
                PageSize = 1000 // TODO: Replace this with continuous streaming with a continuation token 
            }))
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

            return Task.Run(async () => await SearchByPrefixAsync(keyPrefix)).GetAwaiter().GetResult().ToHashSet();
        }

        public IDictionary<string, HashSet<string>> SearchManyByPrefix(IEnumerable<string> keysPrefixes, int? depthIndex = null, string delimiter = null)
        {
            var tasks = keysPrefixes.Select(keyPrefix => (keyPrefix, Task.Run(() => SearchByPrefixAsync(keyPrefix)))).ToArray();
            Task.WaitAll(tasks.Select(kv => kv.Item2 as Task).ToArray());

            return tasks.ToDictionary(t => t.Item1, t => t.Item2.GetAwaiter().GetResult().ToHashSet());
        }

        public void Dispose()
        {
            _channel.ShutdownAsync().Wait();
        } //=> _lightClient.Dispose();
    }
}
