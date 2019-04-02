using System;
using System.Runtime.Serialization;

namespace LmdbCacheServer
{
    [Serializable]
    internal class MessageFormatException : Exception
    {
        public MessageFormatException()
        {
        }

        public MessageFormatException(string message) : base(message)
        {
        }

        public MessageFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MessageFormatException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}