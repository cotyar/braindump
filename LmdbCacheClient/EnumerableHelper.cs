using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace LmdbCacheClient
{
    public static class EnumerableHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable) => new HashSet<T>(enumerable);
    }
}
