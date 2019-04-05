using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using LmdbCache;
using LZ4;

namespace LmdbCacheClient
{
    public class ValueCompressor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Stream CompressGZip(Stream stream) => new GZipStream(stream, CompressionLevel.Fastest);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Stream CompressLz4(Stream stream) => new LZ4Stream(stream, LZ4StreamMode.Compress);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Stream Compress(KvMetadata.Types.Compression compressionLevel, Stream stream)
        {
            switch (compressionLevel)
            {
                case KvMetadata.Types.Compression.None:
                    return stream;
                case KvMetadata.Types.Compression.Lz4:
                    return CompressLz4(stream);
                case KvMetadata.Types.Compression.Gzip:
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
        public Stream Decompress(KvMetadata.Types.Compression compressionLevel, Stream stream)
        {
            switch (compressionLevel)
            {
                case KvMetadata.Types.Compression.None:
                    return stream;
                case KvMetadata.Types.Compression.Lz4:
                    return DecompressLz4(stream);
                case KvMetadata.Types.Compression.Gzip:
                    return DecompressGZip(stream);
                default:
                    throw new ArgumentOutOfRangeException(nameof(compressionLevel), compressionLevel, null);
            }
        }

    }
}
