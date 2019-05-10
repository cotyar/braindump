using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LmdbCacheClient
{
    public class LightWriteStream : Stream
    {
        private readonly Stream _stream;
        private int _length;

        public LightWriteStream(Stream stream)
        {
            _stream = stream;
            _length = 0;
        }

        public override void Flush() => _stream.Flush();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
            _length += count;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _length;

        public override long Position
        {
            get => Length;
            set => throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            _stream.Dispose();
        }
    }
}
