using System.Collections.Generic;
using System.Threading.Tasks;
using LmdbCache;
using LmdbLight;

namespace LmdbCacheServer
{
    public interface IKvTable
    {
        bool ContainsKey(TableKey key);
        (TableKey, bool)[] ContainsKeys(TableKey[] keys);

        /// <summary>
        /// NOTE: Long running keys enumerable will prevent ReadOnly transaction from closing which can affect efficiency of page reuse in the DB  
        /// </summary>
        (TableKey, TableValue?)[] Get(IEnumerable<TableKey> keys);

        TableKey[] KeysByPrefix(TableKey prefix, uint page, uint pageSize);
        (TableKey, TableValue)[] PageByPrefix(TableKey prefix, uint page, uint pageSize);
        Task<bool> Add(TableKey key, TableValue value, bool requiresIsolation = false);
        Task<bool> AddOrUpdate(TableKey key, TableValue value, bool requiresIsolation = false);
        Task<(TableKey, bool)[]> AddBatch((TableKey, TableValue)[] batch, bool requiresIsolation = false);
        Task<(TableKey, bool)[]> AddOrUpdateBatch((TableKey, TableValue)[] batch, bool requiresIsolation = false);
        Task<CopyResponse> Copy(CopyRequest request);
        Task<(TableKey, bool)[]> Delete(TableKey[] keys);
    }
}