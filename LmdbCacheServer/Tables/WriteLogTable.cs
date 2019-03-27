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
        private readonly Func<(ulong, VectorClock)> _initClock;
        private readonly Func<(ulong, VectorClock), (ulong, VectorClock)> _incrementClock;
        private readonly Table _table;

        public WriteLogTable(LightningPersistence lmdb, string tableName, Func<(ulong, VectorClock)> initClock, Func<(ulong, VectorClock), (ulong, VectorClock)> incrementClock)
        {
            _lmdb = lmdb;
            _initClock = initClock;
            _incrementClock = incrementClock;
            _table = _lmdb.OpenTable(tableName);
        }

        public (ulong, VectorClock) GetLastClock(AbstractTransaction txn)
        {
            var ret = txn.TryGetLast(_table);
            return ret.HasValue 
                ? (FromTableKey(ret.Value.Item1), FromTableValue(ret.Value.Item2).Clock)
                : _initClock();
        }

        public void AddLogEvents(WriteTransaction txn, WriteLogEvent[] logEvents)
        {
            var vClock = GetLastClock(txn);
            txn.AddBatch(_table,
                logEvents.Select(logEvent =>
                {
                    vClock = _incrementClock(vClock);
                    logEvent.Clock = vClock.Item2;
                    return (ToTableKey(vClock.Item1), ToTableValue(logEvent));
                }).ToArray());
        }

        public (ulong, WriteLogEvent)[] GetLogPage(AbstractTransaction txn, ulong startFrom, uint pageSize) => 
            txn.PageByPrefix(_table, ToTableKey(startFrom), 0, pageSize).Select(kv => (FromTableKey(kv.Item1), FromTableValue(kv.Item2))).ToArray();

        public TableKey ToTableKey(ulong version) => version.ToBytes();
        public TableValue ToTableValue(WriteLogEvent logEvent) => logEvent.ToByteArray();

        public ulong FromTableKey(TableKey key) => key.Key.ToByteArray().ToUint64();
        public WriteLogEvent FromTableValue(TableValue value) => WriteLogEvent.Parser.ParseFrom(value);
    }
}
