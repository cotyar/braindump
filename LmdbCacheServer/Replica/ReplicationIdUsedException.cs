using System;
using System.Runtime.Serialization;

namespace LmdbCacheServer.Replica
{
    [Serializable]
    public class ReplicationIdUsedException : Exception
    {
        public ReplicationIdUsedException()
        {
        }

        protected ReplicationIdUsedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public ReplicationIdUsedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ReplicationIdUsedException(string requestReplicaId) : base(requestReplicaId)
        {
        }
    }
}