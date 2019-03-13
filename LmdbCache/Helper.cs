using System;

namespace LmdbCache
{
    public static class Helper
    {
        public static Timestamp ToTimestamp(this DateTimeOffset dt) => new Timestamp {TicksOffsetUtc = dt.UtcTicks};
    }
}
