using System;
using System.Net;
using Google.Protobuf;

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

        public static T[] Concat<T>(this T x, params T[][] y)
        {
            if (x == null) throw new ArgumentNullException(nameof(x));
            if (y == null) throw new ArgumentNullException(nameof(y));
            var totalLen = 1;
            // ReSharper disable once LoopCanBeConvertedToQuery
            // Disabled for performance
            foreach (var yi in y)
            {
                totalLen += yi.Length;
            }

            var ret = new T[totalLen];
            ret[0] = x;
            var accLen = 1;
            foreach (var yi in y)
            {
                Array.Copy(yi, 0, ret, accLen, yi.Length);
                accLen += yi.Length;
            }
            return ret;
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

            if (x.Length < y.Length) return false;

            // ReSharper disable once LoopCanBeConvertedToQuery
            // Disabled for performance
            for (int i = 0; i < y.Length; i++)
            {
                if (x[i] != y[i]) return false;
            }

            return true;
        }

        public static bool StartsWith<T>(this ByteString x, ByteString y)
        {
            if (x == null) throw new ArgumentNullException(nameof(x));
            if (y == null) throw new ArgumentNullException(nameof(y));

            if (x.Length < y.Length) return false;

            // ReSharper disable once LoopCanBeConvertedToQuery
            // Disabled for performance
            for (int i = 0; i < y.Length; i++)
            {
                if (x[i] != y[i]) return false;
            }

            return true;
        }

        // TODO: Deal with uint and ulong properly
        public static byte[] ToBytes(this int index) => BitConverter.GetBytes(IPAddress.HostToNetworkOrder(index));
        public static byte[] ToBytes(this uint index) => BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)index));
        public static byte[] ToBytes(this long index) => BitConverter.GetBytes(IPAddress.HostToNetworkOrder(index));
        public static byte[] ToBytes(this ulong index) => BitConverter.GetBytes(IPAddress.HostToNetworkOrder((long)index));

        public static int ToInt32(this byte[] bytes, int startIndex = 0) => IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, startIndex));
        public static uint ToUint32(this byte[] bytes, int startIndex = 0) => (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, startIndex));
        public static long ToInt64(this byte[] bytes, int startIndex = 0) => IPAddress.NetworkToHostOrder(BitConverter.ToInt64(bytes, startIndex));
        public static ulong ToUint64(this byte[] bytes, int startIndex = 0) => (ulong)IPAddress.NetworkToHostOrder(BitConverter.ToInt64(bytes, startIndex));
    }
}
