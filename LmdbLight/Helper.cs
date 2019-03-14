using System;
using System.Net;

namespace LmdbLight
{
    public static class Helper
    {
        public static T[] Concat<T>(this T[] x, T[] y)
        {
            if (x == null) throw new ArgumentNullException(nameof(x));
            if (y == null) throw new ArgumentNullException(nameof(y));
            int oldLen = x.Length;
            Array.Resize<T>(ref x, x.Length + y.Length);
            Array.Copy(y, 0, x, oldLen, y.Length);
            return x;
        }

        public static T[] Concat<T>(this T[] x, params T[][] y)
        {
            if (x == null) throw new ArgumentNullException(nameof(x));
            if (y == null) throw new ArgumentNullException(nameof(y));
            var oldLen = x.Length;
            var totalLen = oldLen;
            // ReSharper disable once LoopCanBeConvertedToQuery
            // Disabled for performance
            foreach (var yi in y)
            {
                totalLen += yi.Length;
            }

            Array.Resize<T>(ref x, totalLen);
            var accLen = oldLen;
            foreach (var yi in y)
            {
                Array.Copy(yi, 0, x, accLen, yi.Length);
                accLen += yi.Length;
            }
            return x;
        }

        public static bool StartsWith<T>(this byte[] x, byte[] y)
        {
            if (x == null) throw new ArgumentNullException(nameof(x));
            if (y == null) throw new ArgumentNullException(nameof(y));

            if (x.Length > y.Length) return false;

            // ReSharper disable once LoopCanBeConvertedToQuery
            // Disabled for performance
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i]) return false;
            }

            return true;
        }

        public static byte[] ToBytes(this uint index) => BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)index));
        public static byte[] ToBytes(this long index) => BitConverter.GetBytes(IPAddress.HostToNetworkOrder(index));

        public static uint ToUint32(this byte[] bytes) => (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 0));

    }
}
