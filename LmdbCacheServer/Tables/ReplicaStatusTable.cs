﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using LmdbCache;
using LmdbLight;

namespace LmdbCacheServer
{
    public class ReplicaStatusTable
    {
        private readonly LightningPersistence _lmdb;
        private readonly string _replicaId;
        private readonly Table _table;

        private readonly TableKey _clockKey;
        private readonly TableKey _countersKey;

        public const string KEY_CLOCK = "KEY_CLOCK";
        public const string KEY_COUNTERS = "KEY_COUNTERS";

        public ReplicaStatusTable(LightningPersistence lmdb, string tableName, string replicaId)
        {
            _lmdb = lmdb;
            _replicaId = replicaId;
            _table = _lmdb.OpenTable(tableName);

            _clockKey = new TableKey(KEY_CLOCK);
            _countersKey = new TableKey(KEY_COUNTERS);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReplicaCounters GetCounters(AbstractTransaction txn)
        {
            var ret = txn.TryGet(_table, _countersKey);
            return ret.HasValue
                ? FromCountersTableValue(ret.Value)
                : new ReplicaCounters ();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetCounters(WriteTransaction txn, ReplicaCounters counters) => 
            txn.AddOrUpdate(_table, _countersKey, ToCountersTableValue(counters));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VectorClock GetLastClock(AbstractTransaction txn)
        {
            var ret = txn.TryGet(_table, _clockKey);
            return ret.HasValue
                ? FromClockTableValue(ret.Value)
                : VectorClockHelper.Create(_replicaId, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetLastClock(WriteTransaction txn, VectorClock clock) =>
            txn.AddOrUpdate(_table, _clockKey, ToClockTableValue(clock));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IncrementClock(WriteTransaction txn, Func<VectorClock, VectorClock> clock)
        {
            var lastClock = GetLastClock(txn);
            var newClock = clock(lastClock);
            return SetLastClock(txn, newClock);
        }

        /// <summary>
        /// No lambdas for performance reasons.
        /// </summary>
        /// <param name="txn"></param>
        /// <param name="addsCounter"></param>
        /// <param name="deletesCounter"></param>
        /// <param name="getCounter"></param>
        /// <param name="containsCounter"></param>
        /// <param name="copysCounter"></param>
        /// <param name="keySearchCounter"></param>
        /// <param name="metadataSearchCounter"></param>
        /// <param name="pageSearchCounter"></param>
        /// <param name="largestKeySize"></param>
        /// <param name="largestValueSize"></param>
        /// <param name="replicatedAdds"></param>
        /// <param name="replicatedDeletes"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IncrementCounters(WriteTransaction txn,
            uint addsCounter = 0, uint deletesCounter = 0, uint copysCounter = 0, 
            uint getCounter = 0, uint containsCounter = 0, 
            uint keySearchCounter = 0, uint metadataSearchCounter = 0, uint pageSearchCounter = 0,
            uint largestKeySize = 0, uint largestValueSize = 0,
            uint replicatedAdds = 0, uint replicatedDeletes = 0)
        {
            var counters = GetCounters(txn);

            if (addsCounter != 0) counters.AddsCounter += addsCounter;
            if (deletesCounter != 0) counters.DeletesCounter += deletesCounter;
            if (copysCounter != 0) counters.CopysCounter += copysCounter;

            if (getCounter != 0) counters.GetCounter += getCounter;
            if (containsCounter != 0) counters.ContainsCounter += containsCounter;
            if (keySearchCounter != 0) counters.KeySearchCounter += keySearchCounter;
            if (pageSearchCounter != 0) counters.PageSearchCounter += pageSearchCounter;
            if (metadataSearchCounter != 0) counters.MetadataSearchCounter += metadataSearchCounter;

            if (largestKeySize != 0) counters.LargestKeySize += largestKeySize;
            if (largestValueSize != 0) counters.LargestValueSize += largestValueSize;

            if (replicatedAdds != 0) counters.ReplicatedAdds += replicatedAdds;
            if (replicatedDeletes != 0) counters.ReplicatedDeletes += replicatedDeletes;

            return SetCounters(txn, counters);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IncrementCounters(WriteTransaction txn, Action<ReplicaCounters> countersUpdater)
        {
            var counters = GetCounters(txn);
            countersUpdater(counters);
            return SetCounters(txn, counters);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TableValue ToClockTableValue(VectorClock clock) => clock.ToByteArray();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VectorClock FromClockTableValue(TableValue value) => VectorClock.Parser.ParseFrom(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TableValue ToCountersTableValue(ReplicaCounters counters) => counters.ToByteArray();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReplicaCounters FromCountersTableValue(TableValue value) => ReplicaCounters.Parser.ParseFrom(value);
    }
}
