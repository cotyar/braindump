using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace LmdbCache
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

        public static uint ToUint32(this byte[] bytes) => (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 0));

    }
}
