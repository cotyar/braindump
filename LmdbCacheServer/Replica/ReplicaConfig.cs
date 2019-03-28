using System;
using System.Collections.Generic;
using System.Text;

namespace LmdbCacheServer.Replica
{
    public struct ReplicaConfig
    {
        public string ReplicaId;
        public int Port;
        public string MasterNode;
    }
}
