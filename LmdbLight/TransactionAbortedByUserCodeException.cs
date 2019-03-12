using System;
using System.Runtime.Serialization;

namespace LmdbLight
{
    public class TransactionAbortedByUserCodeException : Exception
    {
        public TransactionAbortedByUserCodeException()
        {
        }

        public TransactionAbortedByUserCodeException(string message) : base(message)
        {
        }

        public TransactionAbortedByUserCodeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TransactionAbortedByUserCodeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}