using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace LmdbCache
{

    /// <summary>
    /// Here's a read-only Stream implementation that uses an IEnumerable<byte> as input
    /// </summary>
    public class ByteStream : Stream, IDisposable
    {
        private readonly IEnumerator<byte> _input;
        private bool _disposed;

        public ByteStream(IEnumerable<byte> input)
        {
            _input = input.GetEnumerator();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;
        public override long Position { get; set; } = 0;

        public override int Read(byte[] buffer, int offset, int count)
        {
            int i = 0;
            for (; i < count && _input.MoveNext(); i++)
                buffer[i + offset] = _input.Current;
            return i;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new InvalidOperationException();
        public override void SetLength(long value) => throw new InvalidOperationException();
        public override void Write(byte[] buffer, int offset, int count) => throw new InvalidOperationException();
        public override void Flush() => throw new InvalidOperationException();

        void IDisposable.Dispose()
        {
            if (_disposed)
                return;
            _input.Dispose();
            _disposed = true;
        }
    }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Stream ToStream(this IEnumerable<byte> bytes) => new ByteStream(bytes);
    }
}
