using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using LmdbCache;
using LmdbCacheServer.Tables;
using LmdbLight;

namespace LmdbCacheServer
{
    public class KvMetadataTable
    {
        private readonly LightningPersistence _lmdb;
        private readonly Table _table;

        public KvMetadataTable(LightningPersistence lmdb, string tableName)
        {
            _lmdb = lmdb;
            _table = _lmdb.OpenTable(tableName);
        }

        public KvMetadata TryGet(AbstractTransaction txn, KvKey key) => TryGet(txn, ToTableKey(key));

        public KvMetadata TryGet(AbstractTransaction txn, TableKey key)
        {
            var ret = txn.TryGet(_table, key);
            return ret.HasValue
                ? FromTableValue(ret.Value)
                : null;
        }

        public bool AddOrUpdate(WriteTransaction txn, KvKey key, KvMetadata metadata) =>
            txn.AddOrUpdate(_table, ToTableKey(key), ToTableValue(metadata));
        public bool AddOrUpdate(WriteTransaction txn, TableKey key, KvMetadata metadata) =>
            txn.AddOrUpdate(_table, key, ToTableValue(metadata));

        public void Erase(WriteTransaction txn, KvKey key) => txn.Delete(_table, ToTableKey(key));


        public KvKey[] KeysByPrefix(AbstractTransaction txn, KvKey startFrom, uint page, uint pageSize) =>
            txn.KeysByPrefix(_table, ToTableKey(startFrom), page, pageSize).Select(FromTableKey).ToArray();

        public (KvKey, KvMetadata)[] PageByPrefix(AbstractTransaction txn, KvKey startFrom, uint page, uint pageSize) =>
            txn.PageByPrefix(_table, ToTableKey(startFrom), page, pageSize).Select(kv => (FromTableKey(kv.Item1), FromTableValue(kv.Item2))).ToArray();


        public TableKey ToTableKey(KvKey key) => new TableKey(key.Key);
        public TableValue ToTableValue(KvMetadata metadata) => metadata.ToByteArray();

        public KvKey FromTableKey(TableKey key) => new KvKey(key.Key.ToStringUtf8());
        public KvMetadata FromTableValue(TableValue value) => KvMetadata.Parser.ParseFrom(value);
    }
}
