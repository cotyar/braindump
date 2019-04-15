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

        public const string KEY_CLOCK = "CLOCK|";
        public const string KEY_POS = "POS|";

        public ReplicationTable(LightningPersistence lmdb, string tableName)
        {
            _lmdb = lmdb;
//            _replicaId = replicaId;
            _table = _lmdb.OpenTable(tableName);
        }

        /// VectorClock

        public VectorClock GetLastClock(AbstractTransaction txn, string replicaId)
        {
            var ret = txn.TryGet(_table, ToClockTableKey(replicaId));
            return ret.HasValue 
                ? FromClockTableValue(ret.Value)
                : VectorClockHelper.Create(replicaId, 0);
        }

        public (string, VectorClock)[] GetLastClocks(AbstractTransaction txn) =>
            txn.PageByPrefix(_table, ToClockTableKey(KEY_CLOCK), 0, uint.MaxValue).Select(kv => (FromClockTableKey(kv.Item1), FromClockTableValue(kv.Item2))).ToArray();

        public bool SetLastClock(WriteTransaction txn, string replicaId, VectorClock clock) => 
            txn.AddOrUpdate(_table, ToClockTableKey(replicaId), ToClockTableValue(clock));

        /// Pos

        public ulong? GetLastPos(AbstractTransaction txn, string replicaId)
        {
            var ret = txn.TryGet(_table, ToPosTableKey(replicaId));
            return ret.HasValue ? FromPosTableValue(ret.Value) : (ulong?) null;
        }

        public ulong? GetLastPos(string replicaId) => _lmdb.Read(txn => GetLastPos(txn, replicaId));

        public (string, ulong)[] GetLastPos(AbstractTransaction txn) =>
            txn.PageByPrefix(_table, ToClockTableKey(KEY_POS), 0, uint.MaxValue).
                Select(kv => (FromPosTableKey(kv.Item1), FromPosTableValue(kv.Item2))).ToArray();

        public bool SetLastPos(WriteTransaction txn, string replicaId, ulong pos) =>
            txn.AddOrUpdate(_table, ToPosTableKey(replicaId), ToPosTableValue(pos));



        public TableKey ToClockTableKey(string replicaId) => new TableKey(KEY_CLOCK + replicaId);
        public TableValue ToClockTableValue(VectorClock clock) => clock.ToByteArray();

        public string FromClockTableKey(TableKey key) => key.Key.ToStringUtf8();
        public VectorClock FromClockTableValue(TableValue value) => VectorClock.Parser.ParseFrom(value);

        public TableKey ToPosTableKey(string replicaId) => new TableKey(KEY_CLOCK + replicaId);
        public TableValue ToPosTableValue(ulong clock) => clock.ToBytes();

        public string FromPosTableKey(TableKey key) => key.Key.ToStringUtf8();
        public ulong FromPosTableValue(TableValue value) => value.Value.ToByteArray().ToUint64();
    }
}
