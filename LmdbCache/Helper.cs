using System;

namespace LmdbCache
{
    public static class Helper
    {
        public static Timestamp ToTimestamp(this ulong ticks) => new Timestamp {TicksOffsetUtc = ticks};
        public static Timestamp ToTimestamp(this long ticks) => ToTimestamp((ulong) ticks);
        public static Timestamp ToTimestamp(this DateTimeOffset dt) => ToTimestamp(dt.UtcTicks);
    }
}
