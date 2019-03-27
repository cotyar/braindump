using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using LmdbCache;
using LmdbLight;

namespace LmdbCacheServer
{
    public class WriteLogTable
    {
        private readonly LightningPersistence _lmdb;
        private readonly Table _table;

        public WriteLogTable(LightningPersistence lmdb, string tableName)
        {
            _lmdb = lmdb;
            _table = _lmdb.OpenTable(tableName);
        }

        public void AddLogEvents(WriteTransaction txn, WriteLogEvent[] logEvents) => 
            txn.AddBatch(_table, logEvents.Select(expiryKey => (ToTableKey(expiryKey.Item1), ToTableValue(expiryKey.Item2))).ToArray());

        public TableKey ToTableKey(ulong version) => version.ToBytes();
        public TableValue ToTableValue(WriteLogEvent logEvent) => logEvent.ToByteArray();

        public ulong FromTableKey(TableKey key) => key.Key.ToByteArray().ToUint64();
        public WriteLogEvent FromTableValue(TableValue value) => WriteLogEvent.Parser.ParseFrom(value);
    }
}
