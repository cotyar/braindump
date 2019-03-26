using System;
using System.Collections.Generic;
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
        public static Ord Compare(this VectorClock left, VectorClock right)
        {
            return Ord.Lt; // TODO: Implement
        }
    }
}
