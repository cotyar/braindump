using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Google.Protobuf;
using LmdbCache;
using LmdbLight;
using static LmdbCache.KvMetadata.Types.Status;
using static LmdbCache.KvMetadata.Types.UpdateAction;

namespace LmdbCacheServer.Tables
{
    //public enum KvItemType
    //{
    //    Expiry      = 0,
    //    VectorClock = 16,
    //    Data        = 128
    //}

    //public abstract class KvTableEntry
    //{
    //    public KvItemType ItemType { get; }

    //    protected KvTableEntry(KvItemType itemType)
    //    {
    //        ItemType = itemType;
    //    }

    //    public static explicit operator KvTableEntry(byte[] entry)
    //    {
    //        var itemType = (KvItemType) entry[0];
    //        switch (itemType)
    //        {
    //            case KvItemType.Expiry:
    //                return new KvExpiry(entry.ToUint64(1).ToTimestamp());
    //            case KvItemType.VectorClock:
    //                return new KvVectorClock(VectorClock.Parser.ParseFrom(entry, 1, entry.Length - 1));
    //            case KvItemType.Data:
    //                return new KvEntryChunk(entry.ToUint32(1),
    //                    ByteString.CopyFrom(entry, sizeof(uint) + 1, entry.Length - sizeof(uint) - 1));
    //            default:
    //                throw new ArgumentOutOfRangeException();
    //        }
    //    }

    //    public static explicit operator KvTableEntry(TableValue entry) => (KvTableEntry) entry.Value.ToByteArray();

    //    public abstract TableValue ToTableValue();
    //}

    //public class KvExpiry : KvTableEntry
    //{
    //    public Timestamp Expiry { get; }

    //    public KvExpiry(Timestamp expiry) : base(KvItemType.Expiry)
    //    {
    //        Expiry = expiry;
    //    }

    //    public override TableValue ToTableValue() => ((byte)ItemType).Concat(Expiry.ToByteArray());
    //}

    //public class KvVectorClock : KvTableEntry
    //{
    //    public VectorClock VectorClock { get; }

    //    public KvVectorClock(VectorClock vectorClock) : base(KvItemType.VectorClock)
    //    {
    //        VectorClock = vectorClock;
    //    }

    //    public override TableValue ToTableValue() => ((byte)ItemType).Concat(VectorClock.ToByteArray());
    //}

    //public class KvEntryChunk : KvTableEntry
    //{
    //    public uint Index { get; }
    //    public ByteString Value { get; }

    //    public KvEntryChunk(uint index, ByteString value) : base(KvItemType.Data)
    //    {
    //        Index = index;
    //        Value = value;
    //    }

    //    public KvEntryChunk(uint index, byte[] value) : this(index, ByteString.CopyFrom(value)) { }
    //    public KvEntryChunk(uint index, string value) : this(index, ByteString.CopyFromUtf8(value)) { }

    //    public override TableValue ToTableValue() => ((byte) ItemType).Concat(Index.ToBytes(), Value.ToByteArray());
    //}

    public class KvTable //: IKvTable
    {
        private readonly LightningPersistence _lmdb;
        private readonly ExpiryTable _expiryTable;
        private readonly KvMetadataTable _metadataTable;
        private readonly Func<VectorClock> _currentClock;
        private readonly Func<WriteTransaction, VectorClock> _incrementClock;
        private readonly Action<WriteTransaction, WriteLogEvent> _kvUpdateHandler;
        private readonly Table _kvTable;

        public ReplicaStatusTable StatusTable { get; }

        public KvTable(LightningPersistence lmdb, string kvTableName, ReplicaStatusTable statusTable, ExpiryTable expiryTable, KvMetadataTable metadataTable, 
            Func<VectorClock> currentClock, Func<WriteTransaction, VectorClock> incrementClock,
            Action<WriteTransaction, WriteLogEvent> kvUpdateHandler)
        {
            _lmdb = lmdb;
            StatusTable = statusTable;
            _kvTable = _lmdb.OpenTable(kvTableName);
            _expiryTable = expiryTable;
            _metadataTable = metadataTable;
            _currentClock = currentClock;
            _incrementClock = incrementClock;
            _kvUpdateHandler = kvUpdateHandler;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveExpired(WriteTransaction txn, TableKey tableKey, Timestamp keyExpiry)
        {
            //_expirationQueue(txn, _kvTable, key, keyExpiry);
            var currentClock = _currentClock();
            var metadata = new KvMetadata
            {
                Status = Expired,
                Expiry = keyExpiry,
                Action = Updated,
                Updated = currentClock
            };

            txn.Delete(_kvTable, tableKey); // TODO: Check and fail on not successful return codes
            _metadataTable.AddOrUpdate(txn, tableKey, metadata);
            // _kvUpdateHandler(txn, ToDeleteLogEvent(key, metadata)); // TODO: Add EXPIRY-s to the replication log?
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckAndRemoveIfExpired(KvKey key, VectorClock currentClock, KvMetadata metadataToCheck, WriteTransaction txn = null)
        {
            if (metadataToCheck == null || metadataToCheck.Status == Active && currentClock.TicksOffsetUtc < metadataToCheck.Expiry.TicksOffsetUtc)
                return true;

            if (metadataToCheck.Status == Active && currentClock.TicksOffsetUtc < metadataToCheck.Expiry.TicksOffsetUtc)
            {
                if (txn != null)
                {
                    RemoveExpired(txn, ToTableKey(key), metadataToCheck.Expiry);
                }
                else
                {
                    _lmdb.WriteAsync(tx => RemoveExpired(tx, ToTableKey(key), metadataToCheck.Expiry), false);
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KvMetadata TryGetMetadata(AbstractTransaction txn, KvKey key) => _metadataTable.TryGet(txn, key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (KvKey, KvMetadata)[] TryGetMetadata(AbstractTransaction txn, IEnumerable<KvKey> keys) =>
            keys.Select(key => (key, TryGetMetadata(txn, key))).ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (KvKey, KvMetadata)[] TryGetMetadata(KvKey[] keys) => _lmdb.Read(txn => TryGetMetadata(txn, keys));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (KvKey, bool)[] ContainsKeys(KvKey[] keys)
        { 
            var currentTime = _currentClock();
            var ret = TryGetMetadata(keys).
                Select(km => (km.Item1, km.Item2 != null && CheckAndRemoveIfExpired(km.Item1, currentTime, km.Item2))).
                ToArray();

            _lmdb.Write(txn => StatusTable.IncrementCounters(txn, containsCounter: 1), false, true);

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(KvKey key) => ContainsKeys(new [] { key }) [0].Item2;


        /// <summary>
        /// NOTE: Long running keys enumerable will prevent ReadOnly transaction from closing which can affect efficiency of page reuse in the DB  
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (KvKey, KvMetadata, KvValue?)[] Get(IEnumerable<KvKey> keys)
        {
            var ret = _lmdb.Read(txn =>
            {
                var currentTime = _currentClock();
                return keys.Select(key =>
                {
                    var (metadata, value) = TryGet(txn, key, currentTime);
                    return (key, metadata, value);
                }).ToArray();
            });

            _lmdb.Write(txn => StatusTable.IncrementCounters(txn, getCounter: 1), false, true);

            return ret;
        }


        public (KvMetadata, KvValue?) TryGet(AbstractTransaction txn, KvKey key, VectorClock currentTime = null)
        {
            var tableKey = ToTableKey(key);
            var metadata = _metadataTable.TryGet(txn, tableKey);
            if (metadata != null && (currentTime == null || CheckAndRemoveIfExpired(key, currentTime, metadata)))
            {
                var tableEntry = txn.TryGet(_kvTable, tableKey);

                if (tableEntry == null) throw new Exception($"DB inconsistency NO DATA for key: '{key}'");

                var kvValue = FromTableValue(tableEntry.Value);
                return (metadata, kvValue);
            }

            return (metadata, (KvValue?) null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KvKey[] KeysByPrefix(KvKey prefix, uint page, uint pageSize)
        {
            // TODO: Fix pageSize for expiry correction. Use a continuation token.
            var ret = _lmdb.Read(txn =>
            {
                var currentTime = _currentClock();
                return _metadataTable.PageByPrefix(txn, prefix, page, pageSize).
                    Where(ke => CheckAndRemoveIfExpired(ke.Item1, currentTime, ke.Item2)). // TODO: Change to more optimal KvExpiry conversion
                    Select(ke => ke.Item1).
                    ToArray();
            });

            _lmdb.Write(txn => StatusTable.IncrementCounters(txn, keySearchCounter: 1), false, true);

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (KvKey, KvMetadata)[] MetadataByPrefix(AbstractTransaction txn, KvKey prefix, uint page, uint pageSize)
        {
            var ret = _metadataTable.PageByPrefix(txn, prefix, page, pageSize);

            _lmdb.Write(wtxn => StatusTable.IncrementCounters(wtxn, metadataSearchCounter: 1), false, true);

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (KvKey, KvValue)[] PageByPrefix(KvKey prefix, uint page, uint pageSize)
        {
            // TODO: Fix pageSize for expiry correction. Use a continuation token.
            var ret = _lmdb.Read(txn =>
            {
                var currentTime = _currentClock();
                return _metadataTable.PageByPrefix(txn, prefix, page, pageSize).
                    Where(ke => CheckAndRemoveIfExpired(ke.Item1, currentTime, ke.Item2)). 
                    Select(ke => (ke.Item1, FromTableValue(txn.TryGet(_kvTable, ToTableKey(ke.Item1)).Value))). // TODO: Do better exception handling on DB inconsistency
                    ToArray();
            });

            _lmdb.Write(txn => StatusTable.IncrementCounters(txn, pageSearchCounter: 1), false, true);

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(WriteTransaction txn, KvKey key, KvMetadata metadata, KvValue value, VectorClock currentClock, Func<KvKey, VectorClock /*old*/, VectorClock /*new*/, bool> proceedWithUpdatePredicate)
        {
            var tableKey = ToTableKey(key);
            if (metadata.Updated == null) // TODO: Temporal solution. Split KvMetadata into permanent and replica-based ones
            {
                metadata.Updated = _incrementClock(txn);
            }
            var exMetadata = _metadataTable.TryGet(txn, tableKey);
            if (exMetadata != null)
            {
                if (CheckAndRemoveIfExpired(key, currentClock, exMetadata, txn))
                {
                    if (!proceedWithUpdatePredicate(key, exMetadata.Updated, metadata.Updated))
                    {
                        return false;
                    }
                }
            }

            var tableValue = ToTableValue(value);
            txn.AddOrUpdate(_kvTable, tableKey, tableValue); // TODO: Check and fail on not successful return codes
            _metadataTable.AddOrUpdate(txn, tableKey, metadata);
            _expiryTable.AddExpiryRecords(txn, new[] { (metadata.Expiry, tableKey) });
            _kvUpdateHandler(txn, ToAddOrUpdateLogEvent(key, metadata, value));
            StatusTable.IncrementCounters(txn, rc =>
            {
                rc.AddsCounter += 1;
                if (rc.LargestKeySize < (uint)key.Key.Length) rc.LargestKeySize = (uint)key.Key.Length;
                if (rc.LargestValueSize < (uint)value.Value.Length) rc.LargestValueSize = (uint)value.Value.Length;
            });
            return true;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<(KvKey, bool)[]> Add((KvKey, KvMetadata, KvValue)[] batch, Func<KvKey, VectorClock /*old*/, VectorClock/*new*/, bool> proceedWithUpdatePredicate) =>
            _lmdb.WriteAsync(txn =>
            {
                var currentClock = _currentClock();

                return batch.
                    Select(item =>
                    {
                        var (key, metadata, value) = item;
                        return (key, Add(txn, key, metadata, value, currentClock, proceedWithUpdatePredicate));
                    }).
                    ToArray();
            }, false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<(KvKey, bool)[]> Add((KvKey, KvMetadata, KvValue)[] batch) => Add(batch, (key, vcOld, vcNew) => false);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<(KvKey, bool)[]> AddOrUpdate((KvKey, KvMetadata, KvValue)[] batch) => Add(batch, (key, vcOld, vcNew) => vcOld.Earlier(vcNew)); // TODO: Reconfirm conflict resolution

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<bool> Add(KvKey key, KvMetadata metadata, KvValue value)
        {
            var ret = await Add(new[] {(key, metadata, value)});
            return ret[0].Item2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<bool> AddOrUpdate(KvKey key, KvMetadata metadata, KvValue value)
        {
            var ret = await AddOrUpdate(new[] {(key, metadata, value)});
            return ret[0].Item2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                    StatusTable.IncrementCounters(txn, copysCounter: 1);

                    return CopyResponse.Types.CopyResult.Success;
                }).ToArray(), false);

            var response = new CopyResponse();
            response.Results.AddRange(ret);

            return response;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<bool> Delete(KvKey key, KvMetadata metadata)
        {
            var ret = await Delete(new[] { (key, metadata) });
            return ret[0].Item2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<(KvKey, bool)[]> Delete((KvKey, KvMetadata)[] keys) =>
            _lmdb.WriteAsync(txn => 
            {
                var currentClock = _currentClock();

                return keys.
                    Select(item => 
                    {
                        var (key, metadata) = item;
                        if (metadata.Updated == null) // TODO: Temporal solution. Split KvMetadata into permanent and replica-based ones
                        {
                            metadata.Updated = _incrementClock(txn);
                            metadata.Expiry = currentClock.TicksOffsetUtc.ToTimestamp();
                        }
                        var tableKey = ToTableKey(key);
                        var exMetadata = _metadataTable.TryGet(txn, tableKey);
                        if (exMetadata != null)
                        {
                            if (CheckAndRemoveIfExpired(key, currentClock, exMetadata, txn))
                            {
                                if (!exMetadata.Updated.Earlier(metadata.Updated))
                                {
                                    return (key, false);
                                }
                            }
                        }

                        txn.Delete(_kvTable, tableKey); // TODO: Check and fail on not successful return codes
                        _metadataTable.AddOrUpdate(txn, tableKey, metadata);
                        _kvUpdateHandler(txn, ToDeleteLogEvent(key, metadata));
                        StatusTable.IncrementCounters(txn, deletesCounter: 1);
                        return (key, true);
                    }).
                    ToArray();
            }, false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Delete(WriteTransaction txn, KvKey key, KvMetadata metadata) // TODO: Align with other Delete operations (Expiry behaviour is in question)
        { 
            var tableKey = ToTableKey(key);
            var exMetadata = _metadataTable.TryGet(txn, tableKey);
            if (exMetadata?.Status == Active && exMetadata.Updated.Earlier(metadata.Updated)
                || exMetadata?.Status == Expired && exMetadata.Updated.Earlier(metadata.Updated))
            {
                txn.Delete(_kvTable, tableKey);
                _metadataTable.AddOrUpdate(txn, tableKey, metadata);
                _kvUpdateHandler(txn, ToDeleteLogEvent(key, metadata));
                StatusTable.IncrementCounters(txn, deletesCounter: 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TableKey ToTableKey(KvKey key) => new TableKey(key.Key);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TableValue ToTableValue(KvValue value) => new TableValue(value.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KvKey FromTableKey(TableKey key) => new KvKey(key.Key.ToStringUtf8());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KvValue FromTableValue(TableValue value) => new KvValue(value.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WriteLogEvent ToAddOrUpdateLogEvent(KvKey key, KvMetadata metadata, KvValue value)
        {
            //if (value.Value.Length != 1)
            //    throw new ArgumentException(
            //        $"Chunked values are not supported by the WriteLog yet. For key '{key.Key}' received value of '{value.Value.Length}' chunks");

            return new WriteLogEvent { Updated = 
                new WriteLogEvent.Types.AddedOrUpdated
                {
                    Key = key.Key,
                    //Value = value.Value, // TODO: Add streaming // TODO: Implement lazy value logic
                    Expiry = metadata.Expiry
                }};
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WriteLogEvent ToDeleteLogEvent(KvKey key, KvMetadata metadata) => 
            new WriteLogEvent
            {
                Deleted = new WriteLogEvent.Types.Deleted
                {
                    Key = key.Key
                }
            };
    }

    public struct KvKey
    {
        public KvKey(string key)
        {
            Key = key;
        }

        public readonly string Key; // Don't change to a property as they produce IL with callvirt (which affects performance)
    }

//    public struct KvMetadata
//    {
//        public KvMetadata(Timestamp expiry, VectorClock vectorClock)
//        {
//            Expiry = expiry;
//            VectorClock = vectorClock;
//        }
//
//        public Timestamp Expiry { get; }
//
//        public VectorClock VectorClock { get; }
//    }

    public struct KvValue
    {
        public KvValue(ByteString value)
        {
            Value = value;
        }

        public readonly ByteString Value;
    }
}
