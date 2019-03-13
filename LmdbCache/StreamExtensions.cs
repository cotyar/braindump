using System.IO;
using System.Runtime.CompilerServices;

namespace LmdbCache
{
    public static class StreamExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToByteArray(this Stream stream)
        {
            stream.Position = 0;
            var streamLength = (int) stream.Length;
            var buffer = new byte[streamLength];
            for (var totalBytesCopied = 0; totalBytesCopied < streamLength;)
                totalBytesCopied += stream.Read(buffer, totalBytesCopied, streamLength - totalBytesCopied);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Stream ToStream(this byte[] bytes) => new MemoryStream(bytes);
    }
}
