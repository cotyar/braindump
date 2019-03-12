using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LmdbLight
{
    public enum AvailabilityLevel
    {
        SavedToDisk
    }

    public struct DiskUsageInfo
    {
        public int Usage;
    }

    public interface IClient
    {
        HashSet<string> TryAdd(
            IEnumerable<string> keys,
            Action<string, Stream> streamWriter,
            DateTimeOffset expiry,
            AvailabilityLevel requiredAvailabilityLevel = AvailabilityLevel.SavedToDisk);

        HashSet<string> TryAddOrUpdate(
            IEnumerable<string> keys,
            Action<string, Stream> streamWriter,
            DateTimeOffset expiry,
            AvailabilityLevel requiredAvailabilityLevel = AvailabilityLevel.SavedToDisk);

        HashSet<string> TryGet(IEnumerable<string> keys, Action<string, Stream> streamReader);

        void TryCopy(
            IEnumerable<KeyValuePair<string, string>> keyCopies,
            DateTimeOffset expiry,
            out HashSet<string> fromKeysWhichDidNotExist,
            out HashSet<string> toKeysWhichAlreadyExisted,
            AvailabilityLevel requiredAvailabilityLevel = AvailabilityLevel.SavedToDisk);

        HashSet<string> Contains(IEnumerable<string> keys);

        Dictionary<string, Dictionary<string, DiskUsageInfo>> GetDiskUsage(string server = null, int topNKeys = 0, string chunkStore = null);

        /// <returns>Any keys which did not exist, or an empty set otherwise.</returns>
        HashSet<string> TryDelete(IEnumerable<string> keys);

        HashSet<string> SearchByPrefix(string keyPrefix, int? depthIndex = null, string delimiter = null);

        IDictionary<string, HashSet<string>> SearchManyByPrefix(IEnumerable<string> keysPrefixes, int? depthIndex = null, string delimiter = null);
    }
}
