using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LmdbCache.Domain;
using LmdbLight;

namespace LmdbCacheServer
{
    public class LmdbLightClient : IClient, IDisposable
    {
        private readonly LightningPersistence _lmdb;
        private readonly Table _kvTable;

        public LmdbLightClient(LightningConfig config)
        {
            _lmdb = new LightningPersistence(config);
            _kvTable = _lmdb.OpenTable("kv");
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

            var preparedBatch = batch.Select(ks => (new TableKey(ks.Item1), new TableValue(ks.Item2))).ToArray();

            var ret = _lmdb.WriteAsync(txn => allowOverride ? txn.AddOrUpdateBatch(_kvTable, preparedBatch) : txn.AddBatch(_kvTable, preparedBatch), false);
            ret.Wait();

            return new HashSet<string>(ret.Result.Where(kb => kb.Item2).Select(kb => kb.Item1.ToString())); // TODO: Reconfirm if successful or failed keys should be returned
        }

        public HashSet<string> TryAdd(IEnumerable<string> keys, Action<string, Stream> streamWriter, DateTimeOffset expiry,
            AvailabilityLevel requiredAvailabilityLevel = AvailabilityLevel.SavedToDisk) => TryAdd(keys, streamWriter, false, expiry, requiredAvailabilityLevel);

        public HashSet<string> TryAddOrUpdate(IEnumerable<string> keys, Action<string, Stream> streamWriter, DateTimeOffset expiry,
            AvailabilityLevel requiredAvailabilityLevel = AvailabilityLevel.SavedToDisk) => TryAdd(keys, streamWriter, true, expiry, requiredAvailabilityLevel);

        public HashSet<string> TryGet(IEnumerable<string> keys, Action<string, Stream> streamReader)
        {
            var kvs = _lmdb.Get(_kvTable, keys.Select(k => new TableKey(k)));
            var foundKVs = kvs.Where(kv => kv.Item2.HasValue).ToArray();

            foreach (var kv in foundKVs)
            {
                streamReader(kv.Item1.ToString(), kv.Item2.Value.Value.ToByteArray().ToStream());
            }

            return new HashSet<string>(foundKVs.Select(kv => kv.Item1.ToString()));
        }

        public void TryCopy(IEnumerable<KeyValuePair<string, string>> keyCopies, DateTimeOffset expiry, out HashSet<string> fromKeysWhichDidNotExist,
            out HashSet<string> toKeysWhichAlreadyExisted, AvailabilityLevel requiredAvailabilityLevel = AvailabilityLevel.SavedToDisk)
        {
            var (notExists1, existed1) = _lmdb.WriteAsync(txn =>
            {
                var notExists = new List<string>();
                var existed = new List<string>();

                foreach (var fromTo in keyCopies)
                {
                    var from = new TableKey(fromTo.Key);
                    var to = new TableKey(fromTo.Value);

                    if (!txn.ContainsKey(_kvTable, from))
                    {
                        notExists.Add(fromTo.Key);
                    }
                    else if (txn.ContainsKey(_kvTable, to))
                    {
                        existed.Add(fromTo.Value);
                    }
                    else
                    {
                        var val = txn.Get(_kvTable, from);
                        txn.Add(_kvTable, to, val);
                    }
                }

                return (notExists, existed);
            }, false).Result;

            fromKeysWhichDidNotExist = new HashSet<string>(notExists1);
            toKeysWhichAlreadyExisted = new HashSet<string>(existed1);
        }

        public HashSet<string> Contains(IEnumerable<string> keys) =>
            new HashSet<string>(_lmdb.ContainsKeys(_kvTable, keys.Select(k => new TableKey(k)).ToArray()).Where(kv => kv.Item2).Select(kv => kv.Item1.ToString()));

        public Dictionary<string, Dictionary<string, DiskUsageInfo>> GetDiskUsage(string server = null, int topNKeys = 0, string chunkStore = null) =>
            new Dictionary<string, Dictionary<string, DiskUsageInfo>>
            {
                { server, new Dictionary<string, DiskUsageInfo> { { server, new DiskUsageInfo { Usage = 1024 } } } } // TODO: Read from stats
            };
        
        public HashSet<string> TryDelete(IEnumerable<string> keys)
        {
            var kvs = _lmdb.ContainsKeys(_kvTable, keys.Select(k => new TableKey(k)).ToArray());
            var foundKeys = kvs.Where(kv => kv.Item2).Select(kv => kv.Item1).ToArray();
            _lmdb.WriteAsync(txn => txn.Delete(_kvTable, foundKeys), false).Wait();
            return new HashSet<string>(foundKeys.Select(k => k.ToString()));
        }

        public HashSet<string> SearchByPrefix(string keyPrefix, int? depthIndex = null, string delimiter = null)
        {
            // TODO: Deal with depth and delimiter
            // TODO: Rethink pagination

            return new HashSet<string>(_lmdb.KeysByPrefix(_kvTable, keyPrefix, 0, uint.MaxValue).Select(k => k.ToString()));
        }

        public IDictionary<string, HashSet<string>> SearchManyByPrefix(IEnumerable<string> keysPrefixes, int? depthIndex = null, string delimiter = null) => 
            _lmdb.Read(txn => keysPrefixes.Select(keyPrefix => (keyPrefix, txn.KeysByPrefix(_kvTable, keyPrefix, 0, uint.MaxValue)))).
                ToDictionary(x => x.Item1, x => new HashSet<string>(x.Item2.Select(k => k.ToString())));

        public void Dispose() => _lmdb.Dispose();
    }
}
