using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LmdbCache;
using LmdbLight;

namespace LmdbCacheServer
{
    public class KvTable : IKvTable
    {
        private readonly LightningPersistence _lmdb;
        private readonly Table _kvTable;

        public KvTable(LightningPersistence lmdb, string kvTableName)
        {
            _lmdb = lmdb;
            _kvTable = _lmdb.OpenTable(kvTableName);
        }

        public bool ContainsKey(TableKey key) => _lmdb.Read(txn => txn.ContainsKey(_kvTable, key));

        public (TableKey, bool)[] ContainsKeys(TableKey[] keys) => _lmdb.Read(txn => txn.ContainsKeys(_kvTable, keys));


        /// <summary>
        /// NOTE: Long running keys enumerable will prevent ReadOnly transaction from closing which can affect efficiency of page reuse in the DB  
        /// </summary>
        public (TableKey, TableValue?)[] Get(IEnumerable<TableKey> keys) => _lmdb.Get(_kvTable, keys);


        public TableKey[] KeysByPrefix(TableKey prefix, uint page, uint pageSize) =>
            _lmdb.Read(txn => txn.KeysByPrefix(_kvTable, prefix, page, pageSize).ToArray());

        public (TableKey, TableValue)[] PageByPrefix(TableKey prefix, uint page, uint pageSize) =>
            _lmdb.Read(txn => txn.PageByPrefix(_kvTable, prefix, page, pageSize).ToArray());


        public Task<bool> Add(TableKey key, TableValue value, bool requiresIsolation = false) =>
            _lmdb.WriteAsync(txn => txn.Add(_kvTable, key, value), requiresIsolation);
        public Task<bool> AddOrUpdate(TableKey key, TableValue value, bool requiresIsolation = false) =>
            _lmdb.WriteAsync(txn => txn.AddOrUpdate(_kvTable, key, value), requiresIsolation);

        public Task<(TableKey, bool)[]> AddBatch((TableKey, TableValue)[] batch, bool requiresIsolation = false) =>
            _lmdb.WriteAsync(txn => txn.AddBatch(_kvTable, batch), requiresIsolation);
        public Task<(TableKey, bool)[]> AddOrUpdateBatch((TableKey, TableValue)[] batch, bool requiresIsolation = false) =>
            _lmdb.WriteAsync(txn => txn.AddOrUpdateBatch(_kvTable, batch), requiresIsolation);


        public async Task<CopyResponse> Copy(CopyRequest request)
        {
            var ret = await _lmdb.WriteAsync(txn =>
                request.Entries.Select(fromTo =>
                {
                    var from = new TableKey(fromTo.KeyFrom);
                    var to = new TableKey(fromTo.KeyTo);

                    if (!txn.ContainsKey(_kvTable, from))
                    {
                        return CopyResponse.Types.CopyResult.FromKeyNotFound;
                    }
                    else if (txn.ContainsKey(_kvTable, to))
                    {
                        return CopyResponse.Types.CopyResult.ToKeyExists;
                    }
                    else
                    {
                        var val = txn.Get(_kvTable, from);
                        txn.Add(_kvTable, to, val);
                        return CopyResponse.Types.CopyResult.Success;
                    }
                }).ToArray(), false);

            var response = new CopyResponse();
            response.Results.AddRange(ret);

            return response;
        }

        public async Task<(TableKey, bool)[]> Delete(TableKey[] keys)
        {
            var kvResults = await _lmdb.WriteAsync(txn =>
            {
                var kvs = txn.ContainsKeys(_kvTable, keys).ToArray();
                var foundKeys = kvs.Where(kv => kv.Item2).Select(kv => kv.Item1).ToArray();
                txn.Delete(_kvTable, foundKeys);
                return kvs;
            }, false);

            return kvResults;
        }
    }
}
