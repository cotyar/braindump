using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Google.Protobuf;
using LmdbCache;
using LmdbCacheServer.Replica;
using LmdbLight;
using static LmdbCache.KvMetadata.Types.Status;
using static LmdbCache.KvMetadata.Types.UpdateAction;
using static LmdbCache.SyncPacket.Types;

namespace LmdbCacheServer.Tables
{
    public class KvTable
    {
        private readonly LightningPersistence _lmdb;
        private readonly string _replicaId;
        private readonly ExpiryTable _expiryTable;
        private readonly KvMetadataTable _metadataTable;
        private readonly WriteLogTable _wlTable;
        private readonly Func<VectorClock> _currentClock;
        private readonly Func<WriteTransaction, VectorClock> _incrementClock;
        private readonly Func<Item[], Task> _updateNotifier;
        private readonly Table _kvTable;

        public ReplicaStatusTable StatusTable { get; }

        public KvTable(LightningPersistence lmdb, string kvTableName, string replicaId,
            ReplicaStatusTable statusTable, ExpiryTable expiryTable, KvMetadataTable metadataTable, WriteLogTable wlTable,
            Func<VectorClock> currentClock, Func<WriteTransaction, VectorClock> incrementClock, Func<Item[], Task> updateNotifier)
        {
            _lmdb = lmdb;
            _replicaId = replicaId;
            StatusTable = statusTable;
            _kvTable = _lmdb.OpenTable(kvTableName);
            _expiryTable = expiryTable;
            _metadataTable = metadataTable;
            _wlTable = wlTable;
            _currentClock = currentClock;
            _incrementClock = incrementClock;
            _updateNotifier = updateNotifier;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (ulong, VectorClock) IncrementClock(WriteTransaction txn)
        {
            var clock = _incrementClock(txn);
            var posn = clock.GetReplicaValue(_replicaId);
            if (!posn.HasValue)
                throw new EventLogException($"IncrementClock MUST return a new Pos for current replica {_replicaId}. '{clock}'");
            return (posn.Value, clock);
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
                Originated = currentClock,
                LocallyUpdated = currentClock
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
        public (KvKey, KvMetadata)[] MetadataByPrefix(KvKey prefix, uint page, uint pageSize)
        {
            // TODO: Fix pageSize for expiry correction. Use a continuation token.
            return _lmdb.Read(txn => MetadataByPrefix(txn, prefix, page, pageSize));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (KvKey, KvMetadata)[] MetadataByPrefix(AbstractTransaction txn, KvKey prefix, uint page, uint pageSize)
        {
            var ret = _metadataTable.PageByPrefix(txn, prefix, page, pageSize);

            if (txn is WriteTransaction wtxn)
            {
                StatusTable.IncrementCounters(wtxn, metadataSearchCounter: 1);
            }
            else
            {
                _lmdb.Write(wtxn_ => StatusTable.IncrementCounters(wtxn_, metadataSearchCounter: 1), false, true);
            }

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
        public (bool, Item) Add(WriteTransaction txn, KvKey key, KvMetadata metadata, KvValue value, Func<KvKey, VectorClock /*old*/, VectorClock /*new*/, bool> proceedWithUpdatePredicate)
        {
            var tableKey = ToTableKey(key);
            var (pos, clock) = IncrementClock(txn);

            metadata.Originated = metadata.Originated ?? clock;
            metadata.LocallyUpdated = clock;

            var exMetadata = _metadataTable.TryGet(txn, tableKey);
            if (exMetadata != null)
            {
                if (CheckAndRemoveIfExpired(key, clock, exMetadata, txn))
                {
                    if (!proceedWithUpdatePredicate(key, exMetadata.Originated, metadata.Originated))
                    {
                        return (false, null);
                    }
                }
            }

            var tableValue = ToTableValue(value);
            txn.AddOrUpdate(_kvTable, tableKey, tableValue); // TODO: Check and fail on not successful return codes
            _metadataTable.AddOrUpdate(txn, tableKey, metadata);
            _expiryTable.AddExpiryRecords(txn, new[] { (metadata.Expiry, tableKey) });
            var addOrUpdateLogEvent = ToAddOrUpdateLogEvent(key, metadata, value);
            if (!_wlTable.AddLogEvents(txn, addOrUpdateLogEvent))
                throw new EventLogException($"Cannot write event to the WriteLog: '{addOrUpdateLogEvent}'"); // TODO: Values can be large, possibly exclude them from the Exception.
            StatusTable.IncrementCounters(txn, rc =>
            {
                rc.AddsCounter += 1;
                if (rc.LargestKeySize < (uint)key.Key.Length) rc.LargestKeySize = (uint)key.Key.Length;
                if (rc.LargestValueSize < (uint)value.Value.Length) rc.LargestValueSize = (uint)value.Value.Length;
            });
            return (true, new Item { LogEvent = addOrUpdateLogEvent });
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<(KvKey, bool)[]> Add((KvKey, KvMetadata, KvValue)[] batch, Func<KvKey, VectorClock /*old*/, VectorClock/*new*/, bool> proceedWithUpdatePredicate)
        {
            var ret = await _lmdb.WriteAsync(txn =>
            {
                return batch.Select(item =>
                {
                    var (key, metadata, value) = item;
                    return (key, Add(txn, key, metadata, value, proceedWithUpdatePredicate));
                }).ToArray();
            }, false);

            var notifications = ret.Where(r => r.Item2.Item1).Select(r => r.Item2.Item2).ToArray();
            await _updateNotifier(notifications);

            return ret.Select(r => (r.Item1, r.Item2.Item1)).ToArray();
        }

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
        public async Task<(KvKey, bool)[]> Delete((KvKey, KvMetadata)[] keys)
        {
            var ret = await _lmdb.WriteAsync(txn =>
            {
                return keys.Select(item =>
                {
                    var (key, metadata) = item;
                    var (pos, clock) = IncrementClock(txn);

                    metadata.Originated = metadata.Originated ?? clock;
                    metadata.LocallyUpdated = clock;
                    metadata.Expiry = clock.TicksOffsetUtc.ToTimestamp();

                    var tableKey = ToTableKey(key);
                    var exMetadata = _metadataTable.TryGet(txn, tableKey);
                    if (exMetadata != null)
                    {
                        if (CheckAndRemoveIfExpired(key, clock, exMetadata, txn))
                        {
                            if (!exMetadata.Originated.Earlier(metadata.Originated))
                            {
                                return (key, (false, null));
                            }
                        }
                    }

                    txn.Delete(_kvTable, tableKey); // TODO: Check and fail on not successful return codes
                    _metadataTable.AddOrUpdate(txn, tableKey, metadata);
                    var deleteLogEvent = ToDeleteLogEvent(key, metadata);
                    if (!_wlTable.AddLogEvents(txn, deleteLogEvent))
                        throw new EventLogException($"Cannot write event to the WriteLog: '{deleteLogEvent}'"); // TODO: Values can be large, possibly exclude them from the Exception.

                    StatusTable.IncrementCounters(txn, deletesCounter: 1);
                    return (key, (true, new Item { LogEvent = deleteLogEvent }));
                }).ToArray();
            }, false);

            var notifications = ret.Where(r => r.Item2.Item1).Select(r => r.Item2.Item2).ToArray();
            await _updateNotifier(notifications);

            return ret.Select(r => (r.Item1, r.Item2.Item1)).ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Delete(WriteTransaction txn, KvKey key, KvMetadata metadata) // TODO: Align with other Delete operations (Expiry behaviour is in question)
        { 
            var tableKey = ToTableKey(key);
            var (pos, clock) = IncrementClock(txn);

            metadata.Originated = metadata.Originated ?? clock;
            metadata.LocallyUpdated = clock;

            var exMetadata = _metadataTable.TryGet(txn, tableKey);
            if (exMetadata?.Status == Active && exMetadata.Originated.Earlier(metadata.Originated)
                || exMetadata?.Status == Expired && exMetadata.Originated.Earlier(metadata.Originated))
            {
                txn.Delete(_kvTable, tableKey);
                _metadataTable.AddOrUpdate(txn, tableKey, metadata);
                var deleteLogEvent = ToDeleteLogEvent(key, metadata);
                if (!_wlTable.AddLogEvents(txn, deleteLogEvent))
                    throw new EventLogException($"Cannot write event to the WriteLog: '{deleteLogEvent}'"); // TODO: Values can be large, possibly exclude them from the Exception.
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
        public WriteLogEvent ToAddOrUpdateLogEvent(KvKey key, KvMetadata metadata, KvValue value) =>
            new WriteLogEvent {
                Updated = new WriteLogEvent.Types.AddedOrUpdated
                {
                    Key = key.Key,
                    //Value = value.Value, // TODO: Add streaming // TODO: Implement lazy value logic
                    Expiry = metadata.Expiry
                },
                ValueMetadata = metadata.ValueMetadata,
                CorrelationId = metadata.CorrelationId,
                OriginatorReplicaId = _replicaId,
                Originated = metadata.Originated,
                LocallySaved = metadata.LocallyUpdated
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WriteLogEvent ToDeleteLogEvent(KvKey key, KvMetadata metadata) => 
            new WriteLogEvent
            {
                Deleted = new WriteLogEvent.Types.Deleted
                    {
                        Key = key.Key
                    },
                ValueMetadata = metadata.ValueMetadata,
                CorrelationId = metadata.CorrelationId,
                OriginatorReplicaId = _replicaId,
                Originated = metadata.Originated,
                LocallySaved = metadata.LocallyUpdated
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
