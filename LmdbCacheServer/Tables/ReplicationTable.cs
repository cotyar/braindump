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
    public class ReplicationTable
    {
        private readonly LightningPersistence _lmdb;
//        private readonly string _replicaId;
        private readonly Table _table;

        public const string KEY_CLOCK = "KEY_CLOCK|";

        public ReplicationTable(LightningPersistence lmdb, string tableName)
        {
            _lmdb = lmdb;
//            _replicaId = replicaId;
            _table = _lmdb.OpenTable(tableName);
        }

        public VectorClock GetLastClock(AbstractTransaction txn, string replicaId)
        {
            var ret = txn.TryGet(_table, ToClockTableKey(replicaId));
            return ret.HasValue 
                ? FromClockTableValue(ret.Value)
                : VectorClockHelper.Create(replicaId, 0);
        }

        public (string, VectorClock)[] GetLastClocks(AbstractTransaction txn) =>
            txn.PageByPrefix(_table, ToClockTableKey(""), 0, uint.MaxValue).Select(kv => (FromClockTableKey(kv.Item1), FromClockTableValue(kv.Item2))).ToArray();

        public bool SetLastClock(WriteTransaction txn, string replicaId, VectorClock clock) => 
            txn.AddOrUpdate(_table, ToClockTableKey(replicaId), ToClockTableValue(clock));

        public TableKey ToClockTableKey(string replicaId) => new TableKey(/*KEY_CLOCK + */replicaId);
        public TableValue ToClockTableValue(VectorClock clock) => clock.ToByteArray();

        public string FromClockTableKey(TableKey key) => key.Key.ToStringUtf8();
        public VectorClock FromClockTableValue(TableValue value) => VectorClock.Parser.ParseFrom(value);
    }
}
