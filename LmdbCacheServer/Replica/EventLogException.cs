using System;
using System.Runtime.Serialization;

namespace LmdbCacheServer.Replica
{
    [Serializable]
    internal class EventLogException : Exception
    {
        public EventLogException()
        {
        }

        public EventLogException(string message) : base(message)
        {
        }

        public EventLogException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected EventLogException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}