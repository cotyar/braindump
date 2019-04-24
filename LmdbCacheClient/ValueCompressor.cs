using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using LmdbCache;
using LZ4;
using static LmdbCache.ValueMetadata.Types;
using static LmdbCache.ValueMetadata.Types.Compression;

namespace LmdbCacheClient
{
    public class ValueCompressor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Stream CompressGZip(Stream stream) => new GZipStream(stream, CompressionLevel.Fastest);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Stream CompressLz4(Stream stream) => new LZ4Stream(stream, LZ4StreamMode.Compress);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Stream Compress(Compression compressionLevel, Stream stream)
        {
            switch (compressionLevel)
            {
                case None:
                    return stream;
                case Lz4:
                    return CompressLz4(stream);
                case Gzip:
                    return CompressGZip(stream);
                default:
                    throw new ArgumentOutOfRangeException(nameof(compressionLevel), compressionLevel, null);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Stream DecompressGZip(Stream stream) => new GZipStream(stream, CompressionMode.Decompress);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Stream DecompressLz4(Stream stream) => new LZ4Stream(stream, LZ4StreamMode.Decompress);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Stream Decompress(Compression compressionLevel, Stream stream)
        {
            switch (compressionLevel)
            {
                case None:
                    return stream;
                case Lz4:
                    return DecompressLz4(stream);
                case Gzip:
                    return DecompressGZip(stream);
                default:
                    throw new ArgumentOutOfRangeException(nameof(compressionLevel), compressionLevel, null);
            }
        }
    }
}
