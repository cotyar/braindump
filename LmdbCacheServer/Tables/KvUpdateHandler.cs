using System;
using LmdbLight;

namespace LmdbCacheServer.Tables
{
    public class KvUpdateHandler
    {
        private readonly Action _incrementClock;

        public KvUpdateHandler(Action incrementClock)
        {
            _incrementClock = incrementClock;
        }

        public void OnAddOrUpdate(WriteTransaction txn, KvKey key, KvMetadata metadata, KvValue value)
        {
            _incrementClock();
        }

        public void OnDelete(WriteTransaction txn, KvKey key, KvMetadata metadata)
        {
            _incrementClock();
        }
    }
}