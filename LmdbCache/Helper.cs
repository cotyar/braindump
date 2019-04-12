using System;
using System.Threading.Tasks;

namespace LmdbCache
{
    public static class Helper
    {
        public static Timestamp ToTimestamp(this ulong ticks) => new Timestamp {TicksOffsetUtc = ticks};
        public static Timestamp ToTimestamp(this long ticks) => ToTimestamp((ulong) ticks);
        public static Timestamp ToTimestamp(this DateTimeOffset dt) => ToTimestamp(dt.UtcTicks);

        public static Task<T> GrpcSafeHandler<T>(Func<Task<T>> func) =>
            Task.Run(async () =>
            {
                try
                {
                    return await func();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            });

        public static Task GrpcSafeHandler(Func<Task> func) =>
            Task.Run(async () =>
            {
                try
                {
                    await func();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            });

        public static Task GrpcSafeHandler(Action action) => GrpcSafeHandler(() =>
        {
            action();
            return Task.CompletedTask;
        });

        public static Task<T> GrpcSafeHandler<T>(Func<T> func) => GrpcSafeHandler(() => Task.FromResult(func()));
    }
}
