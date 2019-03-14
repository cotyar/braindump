using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            txn.AddBatch(_table, expiryKeys.Select(expiryKey => (new TableKey(expiryKey.Item1.TicksOffsetUtc.ToBytes()), new TableValue(expiryKey.Item2.Key))).ToArray());

        public void DeleteExpiryRecords(WriteTransaction txn, Timestamp[] expiries) =>
            txn.Delete(_table, expiries.Select(expiry => (new TableKey(expiry.TicksOffsetUtc.ToBytes()))).ToArray());
    }
}
