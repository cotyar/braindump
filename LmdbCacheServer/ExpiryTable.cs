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
    public class ExpiryTable
    {
        private readonly LightningPersistence _lmdb;
        private readonly DupTable _table;

        public ExpiryTable(LightningPersistence lmdb, string tableName)
        {
            _lmdb = lmdb;
            _table = _lmdb.OpenDupTable(tableName);
        }

        public void AddExpiryRecords(WriteTransaction txn, (Timestamp, TableKey)[] expiryKeys) => 
            txn.AddBatch(_table, expiryKeys.Select(expiryKey => (ToTableKey(expiryKey.Item1), ToTableValue(expiryKey.Item2))).ToArray());

        public void DeleteExpiryRecords(WriteTransaction txn, Timestamp[] expiries) =>
            txn.Delete(_table, expiries.Select(ToTableKey).ToArray());

        public void ProcessExpired(WriteTransaction txn, Timestamp expiryUntil, uint pageSize, Func<TableKey, Timestamp, bool> tryDeleteInMain)
        {
            var keys = txn.KeysTakeWhile(_table, (key, value) => FromTableKey(key).TicksOffsetUtc <= expiryUntil.TicksOffsetUtc, pageSize);

            foreach (var key in keys)
            {
                var keysToDelete = txn.ReadDuplicatePage(b => ToTableValue(b), _table, key, 0, uint.MaxValue); // TODO: Rethink paging
                foreach (var keyToDelete in keysToDelete)
                {
                    var deleted = tryDeleteInMain(FromTableValue(keyToDelete), expiryUntil); // TODO: Log the keys that hasn't been deleted.
                }

                txn.DeleteAllDuplicates(_table, key); // TODO: Do all the reads and deletes using only one Cursor.
            }

            txn.Delete(_table, keys);
        }

        public TableKey ToTableKey(Timestamp timestamp) => timestamp.TicksOffsetUtc.ToBytes();
        public TableValue ToTableValue(TableKey key) => key.Key.ToByteArray();

        public Timestamp FromTableKey(TableKey key) => key.Key.ToByteArray().ToUint64().ToTimestamp();
        public TableKey FromTableValue(TableValue value) => value.Value;
    }
}
