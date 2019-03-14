using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LmdbLight
{
    public static class LightningHelper
    {
        public static bool ContainsKey(this LightningPersistence lmdb, Table table, TableKey key) => lmdb.Read(txn => txn.ContainsKey(table, key));

        public static (TableKey, bool)[] ContainsKeys(this LightningPersistence lmdb, Table table, TableKey[] keys) => lmdb.Read(txn => txn.ContainsKeys(table, keys));

        public static TableKey[] KeysByPrefix(this LightningPersistence lmdb, Table table, TableKey prefix, uint page, uint pageSize) =>
            lmdb.Read(txn => txn.KeysByPrefix(table, prefix, page, pageSize).ToArray());

        public static (TableKey, TableValue)[] PageByPrefix(this LightningPersistence lmdb, Table table, TableKey prefix, uint page, uint pageSize) => 
            lmdb.Read(txn => txn.PageByPrefix(table, prefix, page, pageSize).ToArray());


        public static Task<bool> Add(this LightningPersistence lmdb, Table table, TableKey key, TableValue value, bool requiresIsolation = false) =>
            lmdb.WriteAsync(txn => txn.Add(table, key, value), requiresIsolation);
        public static Task<bool> AddOrUpdate(this LightningPersistence lmdb, Table table, TableKey key, TableValue value, bool requiresIsolation = false) =>
            lmdb.WriteAsync(txn => txn.AddOrUpdate(table, key, value), requiresIsolation);

        public static Task<(TableKey, bool)[]> AddBatch(this LightningPersistence lmdb, Table table, (TableKey, TableValue)[] batch, bool requiresIsolation = false) =>
            lmdb.WriteAsync(txn => txn.AddBatch(table, batch), requiresIsolation);
        public static Task<(TableKey, bool)[]> AddOrUpdateBatch(this LightningPersistence lmdb, Table table, (TableKey, TableValue)[] batch, bool requiresIsolation = false) =>
            lmdb.WriteAsync(txn => txn.AddOrUpdateBatch(table, batch), requiresIsolation);
    }
}
