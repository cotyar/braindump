using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using LmdbCache;
using LmdbLight;

namespace LmdbCacheServer.Tables
{
    public enum KvItemType
    {
        Expiry      = 0,
        VectorClock = 16,
        Data        = 128
    }

    public abstract class KvTableEntry
    {
        public KvItemType ItemType { get; }

        protected KvTableEntry(KvItemType itemType)
        {
            ItemType = itemType;
        }

        public static explicit operator KvTableEntry(byte[] entry)
        {
            var itemType = (KvItemType) entry[0];
            switch (itemType)
            {
                case KvItemType.Expiry:
                    return new KvExpiry(entry.ToUint64(1).ToTimestamp());
                case KvItemType.VectorClock:
                    return new KvVectorClock(VectorClock.Parser.ParseFrom(entry, 1, entry.Length - 1));
                case KvItemType.Data:
                    return new KvEntryChunk(entry.ToUint32(1),
                        ByteString.CopyFrom(entry, sizeof(uint) + 1, entry.Length - sizeof(uint) - 1));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static explicit operator KvTableEntry(TableValue entry) => (KvTableEntry) entry.Value.ToByteArray();

        public abstract TableValue ToTableValue();
    }

    public class KvExpiry : KvTableEntry
    {
        public Timestamp Expiry { get; }

        public KvExpiry(Timestamp expiry) : base(KvItemType.Expiry)
        {
            Expiry = expiry;
        }

        public override TableValue ToTableValue() => ((byte)ItemType).Concat(Expiry.ToByteArray());
    }

    public class KvVectorClock : KvTableEntry
    {
        public VectorClock VectorClock { get; }

        public KvVectorClock(VectorClock vectorClock) : base(KvItemType.VectorClock)
        {
            VectorClock = vectorClock;
        }

        public override TableValue ToTableValue() => ((byte)ItemType).Concat(VectorClock.ToByteArray());
    }

    public class KvEntryChunk : KvTableEntry
    {
        public uint Index { get; }
        public ByteString Value { get; }

        public KvEntryChunk(uint index, ByteString value) : base(KvItemType.Data)
        {
            Index = index;
            Value = value;
        }

        public KvEntryChunk(uint index, byte[] value) : this(index, ByteString.CopyFrom(value)) { }
        public KvEntryChunk(uint index, string value) : this(index, ByteString.CopyFromUtf8(value)) { }

        public override TableValue ToTableValue() => ((byte) ItemType).Concat(Index.ToBytes(), Value.ToByteArray());
    }

    public class KvTable //: IKvTable
    {
        private readonly LightningPersistence _lmdb;
        private readonly ExpiryTable _expiryTable;
        private readonly Func<VectorClock> _currentClock;
        private readonly Action<WriteTransaction, DupTable, TableKey, Timestamp> _expirationQueue;
        private readonly Action<WriteTransaction, WriteLogEvent> _kvUpdateHandler;
        private readonly DupTable _kvTable;

        public KvTable(LightningPersistence lmdb, string kvTableName, ExpiryTable expiryTable, Func<VectorClock> currentClock, 
            Action<WriteTransaction, DupTable, TableKey, Timestamp> expirationQueue, Action<WriteTransaction, WriteLogEvent> kvUpdateHandler)
        {
            _lmdb = lmdb;
            _kvTable = _lmdb.OpenDupTable(kvTableName);
            _expiryTable = expiryTable;
            _currentClock = currentClock;
            _expirationQueue = expirationQueue;
            _kvUpdateHandler = kvUpdateHandler;
        }

        private void RemoveExpired(WriteTransaction txn, TableKey key, Timestamp keyExpiry) => _expirationQueue(txn, _kvTable, key, keyExpiry);

        private bool CheckAndRemoveIfExpired(KvKey key, VectorClock currentClock, Timestamp timeToCheck)
        {
            //if (currentClock.TicksOffsetUtc < timeToCheck.TicksOffsetUtc)
            {
                return true;
            }

            _lmdb.WriteAsync(txn => RemoveExpired(txn, ToTableKey(key), timeToCheck), true);
            return false;
        }

        private KvMetadata? TryGetMetadata(AbstractTransaction txn, KvKey key)
        {
            var tableKey = ToTableKey(key);
            var tableEntries = txn.ReadDuplicatePage(v => (KvTableEntry)v, _kvTable, tableKey, 0, 2).ToArray();

            return tableEntries.Length > 0
                ? FromTableMetadata(tableEntries)
                : (KvMetadata?)null;
        }

        private (KvKey, KvMetadata?)[] TryGetMetadata(AbstractTransaction txn, IEnumerable<KvKey> keys)
        {
            return keys.
                Select(key => (key, TryGetMetadata(txn, key))).
                ToArray();
        }

        public (KvKey, KvMetadata?)[] TryGetMetadata(KvKey[] keys) => _lmdb.Read(txn => TryGetMetadata(txn, keys));

        public (KvKey, bool)[] ContainsKeys(KvKey[] keys)
        { 
            var currentTime = _currentClock();
            return TryGetMetadata(keys).
                Select(km => (km.Item1, km.Item2.HasValue && CheckAndRemoveIfExpired(km.Item1, currentTime, km.Item2.Value.Expiry))).
                ToArray();
        }

        public bool ContainsKey(KvKey key) => ContainsKeys(new [] { key }) [0].Item2;


        /// <summary>
        /// NOTE: Long running keys enumerable will prevent ReadOnly transaction from closing which can affect efficiency of page reuse in the DB  
        /// </summary>
        public (KvKey, KvValue?)[] Get(IEnumerable<KvKey> keys)
        {
            return _lmdb.Read(txn => 
                {
                    var currentTime = _currentClock();
                    return keys.Select(key =>
                    {
                        var tableKey = ToTableKey(key);
                        var tableEntries = txn.ReadDuplicatePage(v => (KvTableEntry)v, _kvTable, tableKey, 0, uint.MaxValue).ToArray();

                        if (tableEntries.Length > 0)
                        {
                            var metadata = FromTableMetadata(tableEntries);
                            if (CheckAndRemoveIfExpired(key, currentTime, metadata.Expiry))
                            {
                                var kvValue = FromTableValue(tableEntries);
                                return (key, kvValue);
                            }
                        }

                        return (key, (KvValue?) null);
                    }).ToArray();
                });
        }


        public KvKey[] KeysByPrefix(KvKey prefix, uint page, uint pageSize)
        {
            // TODO: Fix pageSize for expiry correction. Use a continuation token.
            return _lmdb.Read(txn =>
            {
                var tableKey = ToTableKey(prefix);
                var currentTime = _currentClock();
                return txn.PageByPrefix(_kvTable, tableKey, page, pageSize).
                    Select(ke => (FromTableKey(ke.Item1), ke.Item2)).
                    Where(ke => CheckAndRemoveIfExpired(ke.Item1, currentTime, ((KvExpiry) (ke.Item2.Value.ToByteArray())).Expiry)). // TODO: Change to more optimal KvExpiry conversion
                    Select(ke => ke.Item1).
                    ToArray();
            });
        }

        public (KvKey, KvValue)[] PageByPrefix(KvKey prefix, uint page, uint pageSize)
        {
            // TODO: Fix pageSize for expiry correction. Use a continuation token.
            return _lmdb.Read(txn =>
            {
                var tableKey = ToTableKey(prefix);
                var currentTime = _currentClock();
                return txn.PageByPrefix(_kvTable, tableKey, page, pageSize).
                    Select(ke => (FromTableKey(ke.Item1), ke.Item2)).
                    Where(ke => CheckAndRemoveIfExpired(ke.Item1, currentTime, ((KvExpiry)ke.Item2.Value.ToByteArray()).Expiry)).
                    // TODO: Rewrite to use only one Cursor
                    Select(ke => (ke.Item1, FromTableValue(txn.ReadDuplicatePage(v => (KvTableEntry)v, _kvTable, tableKey, 0, uint.MaxValue).ToArray()))).
                    ToArray();
            });
        }


        public Task<(KvKey, bool)[]> Add((KvKey, KvMetadata, KvValue)[] batch, Func<KvKey, VectorClock /*old*/, VectorClock/*new*/, bool> proceedWithUpdatePredicate) =>
            _lmdb.WriteAsync(txn =>
            {
                var currentTime = _currentClock();

                return batch.
                    Select(item =>
                    {
                        var (key, metadata, value) = item;
                        var tableKey = ToTableKey(key);
                        var existingMetadata = txn.ReadDuplicatePage(v => (KvTableEntry) v, _kvTable, tableKey, 0, 2).ToArray();
                        if (existingMetadata.Any())
                        {
                            var exMetadata = FromTableMetadata(existingMetadata);
                            if (exMetadata.Expiry.TicksOffsetUtc < currentTime.TicksOffsetUtc)
                            {
                                // Item already expired but hasn't been garbage collected yet
                                RemoveExpired(txn, tableKey, exMetadata.Expiry);
                            }
                            else
                            {
                                if (proceedWithUpdatePredicate(key, exMetadata.VectorClock, metadata.VectorClock))
                                {
                                    txn.DeleteAllDuplicates(_kvTable, tableKey);
                                }
                                else
                                {
                                    return (key, false);
                                }
                            }
                        }

                        var valueEntries = ToTableMetadataArray(metadata).Concat(ToTableEntryChunk(value)).Select(v => (tableKey, v.ToTableValue())).ToArray();
                        txn.AddOrUpdateBatch(_kvTable, valueEntries);
                        _expiryTable.AddExpiryRecords(txn, new [] {(metadata.Expiry, tableKey)});
                        _kvUpdateHandler(txn, ToAddOrUpdateLogEvent(key, metadata, value));
                        return (key, true);
                    }).
                    ToArray();
            }, false);

        public Task<(KvKey, bool)[]> Add((KvKey, KvMetadata, KvValue)[] batch) => Add(batch, (key, vcOld, vcNew) => false);
        public Task<(KvKey, bool)[]> AddOrUpdate((KvKey, KvMetadata, KvValue)[] batch) => Add(batch, (key, vcOld, vcNew) => vcOld.Compare(vcNew) == Ord.Lt); // TODO: Reconfirm conflict resolution

        public async Task<bool> Add(KvKey key, KvMetadata metadata, KvValue value)
        {
            var ret = await Add(new[] {(key, metadata, value)});
            return ret[0].Item2;
        }

        public async Task<bool> AddOrUpdate(KvKey key, KvMetadata metadata, KvValue value)
        {
            var ret = await AddOrUpdate(new[] {(key, metadata, value)});
            return ret[0].Item2;
        }


        public async Task<CopyResponse> Copy(CopyRequest request) // TODO: Redo this completely!!!
        {
            var ret = await _lmdb.WriteAsync(txn =>
                request.Entries.Select(fromTo =>
                {
                    var from = ToTableKey(new KvKey(fromTo.KeyFrom));
                    var to = ToTableKey(new KvKey(fromTo.KeyTo));

                    var val = txn.TryGet(_kvTable, from);
                    if (!val.HasValue)
                    {
                        return CopyResponse.Types.CopyResult.FromKeyNotFound;
                    }

                    if (txn.ContainsKey(_kvTable, to))
                    {
                        return CopyResponse.Types.CopyResult.ToKeyExists;
                    }
                    
                    txn.Add(_kvTable, to, val.Value);
                    return CopyResponse.Types.CopyResult.Success;
                }).ToArray(), false);

            var response = new CopyResponse();
            response.Results.AddRange(ret);

            return response;
        }

        public async Task<bool> Delete(KvKey key)
        {
            var ret = await Delete(new[] {key});
            return ret[0].Item2;
        }

        public async Task<(KvKey, bool)[]> Delete(KvKey[] keys)
        {
            var kvResults = await _lmdb.WriteAsync(txn => 
            {
                var currentTime = _currentClock();

                return keys.
                    Select(key => 
                        {
                            var metadata = TryGetMetadata(txn, key);

                            if (metadata.HasValue && CheckAndRemoveIfExpired(key, currentTime, metadata.Value.Expiry))
                            {
                                txn.Delete(_kvTable, ToTableKey(key)); // TODO: Apply VectorClock logic. Replace with soft delete.
                                _kvUpdateHandler(txn, ToDeleteLogEvent(key, metadata.Value));
                                return (key, true);
                            }

                            return (key, false);
                        }).
                    ToArray();
            }, false);

            return kvResults;
        }


        public TableKey ToTableKey(KvKey key) => new TableKey(key.Key);
        public (KvExpiry, KvVectorClock) ToTableMetadata(KvMetadata metadata) => (new KvExpiry(metadata.Expiry), new KvVectorClock(metadata.VectorClock));
        public KvTableEntry[] ToTableMetadataArray(KvMetadata metadata) => new KvTableEntry[] { new KvExpiry(metadata.Expiry), new KvVectorClock(metadata.VectorClock) };
        public KvEntryChunk[] ToTableEntryChunk(KvValue value) => value.Value.Select((v, i) => new KvEntryChunk((uint)i, v)).ToArray();

        public KvKey FromTableKey(TableKey key) => new KvKey(key.Key.ToStringUtf8());
        public KvMetadata FromTableMetadata(KvExpiry expiry, KvVectorClock vectorClock) => new KvMetadata(new Timestamp(expiry.Expiry), new VectorClock(vectorClock.VectorClock));
        public KvMetadata FromTableMetadata(KvTableEntry[] metadata) => FromTableMetadata((KvExpiry)metadata[0], (KvVectorClock)metadata[1]);
        public KvValue FromTableValue(KvTableEntry[] value) => new KvValue(value.Skip(2).Select(v => ((KvEntryChunk) v).Value).ToArray());

        public WriteLogEvent ToAddOrUpdateLogEvent(KvKey key, KvMetadata metadata, KvValue value)
        {
            if (value.Value.Length != 1)
                throw new ArgumentException(
                    $"Chunked values are not supported by the WriteLog yet. For key '{key.Key}' received value of '{value.Value.Length}' chunks");

            return new WriteLogEvent { Updated = 
                new WriteLogEvent.Types.AddedOrUpdated
                {
                    Key = key.Key,
                    Value = value.Value[0],
                    Expiry = metadata.Expiry
                }};
        }

        public WriteLogEvent ToDeleteLogEvent(KvKey key, KvMetadata metadata) => 
            new WriteLogEvent
            {
                Deleted = new WriteLogEvent.Types.Deleted
                {
                    Key = key.Key
                }
            };

        //        new Timestamp(value[0].ItemType == KvItemType.Expiry? ((KvExpiry) value[0]).Expiry : throw new Exception("Expiry Data value corrupted")), // TODO: Introduce data consistency Exception type
        //        new VectorClock(value[1].ItemType == KvItemType.VectorClock? ((KvVectorClock) value[1]).VectorClock : throw new Exception("VectorClock value corrupted")),
        //        value.Skip(2).Select(v => v.ItemType == KvItemType.Data? ((KvEntryChunk) v).Value : throw new Exception("VectorClock value corrupted")).ToArray()

    }

    public struct KvKey
    {
        public KvKey(string key)
        {
            Key = key;
        }

        public string Key { get; }
    }

    public struct KvMetadata
    {
        public KvMetadata(Timestamp expiry, VectorClock vectorClock)
        {
            Expiry = expiry;
            VectorClock = vectorClock;
        }

        public Timestamp Expiry { get; }

        public VectorClock VectorClock { get; }
    }

    public struct KvValue
    {
        public KvValue(ByteString[] value)
        {
            Value = value;
        }

        public ByteString[] Value { get; }
    }
}
