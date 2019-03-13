using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Google.Protobuf;

namespace LmdbCache.Domain
{
    public partial class LmdbCacheServiceClient
    {

    }

    public interface ILightClient : IDisposable
    {
        Task<string[]> TryAdd(bool overrideExisting, (string, DateTimeOffset, byte[])[] entries);

        Task<(string, byte[])[]> TryGet(string[] keys);
        //Task<GetResponse> TryGet(string[] keys);

        Task<(string[], string[])> TryCopy((string, DateTimeOffset, string)[] keyCopies);

        Task<string[]> Contains(string[] keys);

        /// <returns>Any keys which did not exist, or an empty set otherwise.</returns>
        Task<string[]> TryDelete(string[] keys);

        Task<(string, string[])> SearchByPrefix(string keysPrefix); // TODO: , int? depthIndex = null, string delimiter = null | Add regex?
    }

    public interface ILightAdminClient
    {
        Task<Dictionary<string, Dictionary<string, DiskUsageInfo>>> GetDiskUsage(string server = null, int topNKeys = 0, string chunkStore = null);
    }
}
