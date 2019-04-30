using System.Threading.Tasks;
using LmdbCache;

namespace LmdbCacheServer.Replica
{
    public interface IReplicator
    {
        Task PostWriteLogEvent(SyncPacket.Types.Item syncItem);
    }
}