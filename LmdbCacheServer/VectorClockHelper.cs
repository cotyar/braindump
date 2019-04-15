using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using LmdbCache;

namespace LmdbCacheServer
{
    public enum Ord
    {
        Lt = -1, // lower
        Eq = 0, // equal
        Gt = 1, // greater
        Cc = 2 // concurrent
    }

    public static class VectorClockHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VectorClock CreateEmpty() => new VectorClock();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VectorClock Create(string replicaId, ulong replicaValue) => new VectorClock().SetReplicaValue(replicaId, replicaValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VectorClock Create(IEnumerable<(string, ulong)> replicaValues) => new VectorClock().SetReplicaValues(replicaValues);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VectorClock Increment(this VectorClock oldVectorClock, string replicaId)
        {
            var newVectorClock = oldVectorClock.SetTimeNow();
            var replicas = newVectorClock.Replicas;
            if (replicas.TryGetValue(replicaId, out var replicaValue))
            {
                replicas[replicaId] = replicaValue + 1;
            }
            else
            {
                replicas[replicaId] = 1;
            }

            return newVectorClock;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VectorClock SetReplicaValue(this VectorClock oldVectorClock, string replicaId, ulong replicaValue)
        {
            var newVectorClock = oldVectorClock.Clone();
            newVectorClock.Replicas[replicaId] = replicaValue;
            return newVectorClock;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VectorClock SetTime(this VectorClock oldVectorClock, Timestamp timestamp)
        {
            var newVectorClock = oldVectorClock.Clone();
            newVectorClock.TicksOffsetUtc = timestamp.TicksOffsetUtc;
            return newVectorClock;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VectorClock SetTimeNow(this VectorClock oldVectorClock) => oldVectorClock.SetTime(DateTimeOffset.UtcNow.ToTimestamp());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VectorClock SetReplicaValues(this VectorClock oldVectorClock, IEnumerable<(string, ulong)> replicaValues)
        {
            var newVectorClock = oldVectorClock.Clone();
            foreach (var replicaValue in replicaValues)
            {
                newVectorClock.Replicas[replicaValue.Item1] = replicaValue.Item2;
            }
            return newVectorClock;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong? GetReplicaValue(this VectorClock oldVectorClock, string replicaId) => 
            oldVectorClock.Replicas.TryGetValue(replicaId, out var replicaValue)
                ? replicaValue
                : (ulong?) null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VectorClock Merge(this VectorClock left, VectorClock right) // TODO: Performance critical. Optimize for performance. 
        {
            var newVectorClock = CreateEmpty();
            var replicas = newVectorClock.Replicas;

            foreach (var replicaId in left.Replicas.Keys.Union(right.Replicas.Keys, StringComparer.OrdinalIgnoreCase))
            {
                left.Replicas.TryGetValue(replicaId, out var leftValue);
                right.Replicas.TryGetValue(replicaId, out var rightValue);
                replicas[replicaId] = Math.Max(leftValue, rightValue);
            }

            return newVectorClock;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Ord Compare(this VectorClock left, VectorClock right) // TODO: Performance critical. Optimize for performance.
        {
            var ordLt = true;
            var ordGt = true;

            foreach (var replicaId in left.Replicas.Keys.Union(right.Replicas.Keys, StringComparer.OrdinalIgnoreCase))
            {
                left.Replicas.TryGetValue(replicaId, out var leftValue);
                right.Replicas.TryGetValue(replicaId, out var rightValue);

                if (leftValue < rightValue)
                {
                    ordGt = false;
                }

                if (leftValue > rightValue)
                {
                    ordLt = false;
                }

                if (!ordLt && !ordGt) return Ord.Cc;
            }

            if (!ordGt) return Ord.Lt;
            if (!ordLt) return Ord.Gt;

            if (left.TicksOffsetUtc < right.TicksOffsetUtc) return Ord.Lt;
            if (left.TicksOffsetUtc > right.TicksOffsetUtc) return Ord.Gt;

            return Ord.Eq; 
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Earlier(this VectorClock left, VectorClock right) => left.Compare(right) == Ord.Lt;
    }
}
