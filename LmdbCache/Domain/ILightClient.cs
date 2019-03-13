using System;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;

namespace LmdbCache.Domain
{
    public interface ILightClient
    {
        string[] TryAdd(bool overrideExisting, (string, ByteString, DateTimeOffset)[] entries);

        (string, ByteString)[] TryGet(string[] keys);

        (string[], string[]) TryCopy((string, string, DateTimeOffset)[] keyCopies);

        string[] Contains(string[] keys);

        /// <returns>Any keys which did not exist, or an empty set otherwise.</returns>
        string[] TryDelete(string[] keys);

        (string, string[]) SearchByPrefix(string keysPrefix); // TODO: , int? depthIndex = null, string delimiter = null | Add regex?
    }

    public interface ILightAdminClient
    {
        Dictionary<string, Dictionary<string, DiskUsageInfo>> GetDiskUsage(string server = null, int topNKeys = 0, string chunkStore = null);
    }
}
