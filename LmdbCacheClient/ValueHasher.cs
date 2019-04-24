using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using LmdbCache;
using LZ4;
using static LmdbCache.ValueMetadata.Types;
using static LmdbCache.ValueMetadata.Types.Compression;

namespace LmdbCacheClient
{
    public class ValueHasher : IDisposable
    {
        private readonly MD5 _md5 = MD5.Create();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ComputeMD5(Stream stream) => _md5.ComputeHash(stream);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ComputeMD5(byte[] bytes) => _md5.ComputeHash(bytes);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ComputeHash(HashedWith hashedWith, Stream stream)
        {
            switch (hashedWith)
            {
                case HashedWith.Md5:
                    return ComputeMD5(stream);
                default:
                    throw new ArgumentOutOfRangeException(nameof(hashedWith), hashedWith, null);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ComputeHash(HashedWith hashedWith, byte[] bytes)
        {
            switch (hashedWith)
            {
                case HashedWith.Md5:
                    return ComputeMD5(bytes);
                default:
                    throw new ArgumentOutOfRangeException(nameof(hashedWith), hashedWith, null);
            }
        }

        public void Dispose()
        {
            _md5.Dispose();
        }
    }
}
