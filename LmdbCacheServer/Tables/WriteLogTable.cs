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
        private readonly string _replicaId;
        private readonly Table _table;

        public WriteLogTable(LightningPersistence lmdb, string tableName, string replicaId)
        {
            _lmdb = lmdb;
            _replicaId = replicaId;
            _table = _lmdb.OpenTable(tableName);
        }

        public (ulong, VectorClock)? GetLastClock(AbstractTransaction txn)
        {
            var ret = txn.TryGetLast(_table);
            return ret.HasValue 
                ? (FromTableKey(ret.Value.Item1), FromTableValue(ret.Value.Item2).LocallySaved)
                : ((ulong, VectorClock)?) null;
        }

        public bool AddLogEvents(WriteTransaction txn, WriteLogEvent logEvent)
        {
            var replicaValue = logEvent.LocallySaved.GetReplicaValue(_replicaId);
            if (!replicaValue.HasValue)
                throw new ArgumentException(nameof(logEvent), $"VectorClock for the event is not properly prepared and empty for this replicaId: '{_replicaId}'");

            return txn.Add(_table, ToTableKey(replicaValue.Value), ToTableValue(logEvent));
        }

        public (ulong, WriteLogEvent)[] GetLogPage(AbstractTransaction txn, ulong startFrom, uint pageSize) => 
            txn.PageByPrefix(_table, ToTableKey(startFrom), 0, pageSize).Select(kv => (FromTableKey(kv.Item1), FromTableValue(kv.Item2))).ToArray();

        public TableKey ToTableKey(ulong version) => version.ToBytes();
        public TableValue ToTableValue(WriteLogEvent logEvent) => logEvent.ToByteArray();

        public ulong FromTableKey(TableKey key) => key.Key.ToByteArray().ToUint64();
        public WriteLogEvent FromTableValue(TableValue value) => WriteLogEvent.Parser.ParseFrom(value);
    }
}
