using System;
using System.IO;
using System.Linq;
using LmdbCache;
using LmdbCacheClient;
using LmdbCacheServer;
using NUnit.Framework;
using static LmdbCache.ValueMetadata.Types;

namespace LmdbLightTest
{
    public class ValueCompressorTests
    {

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestValueCompressor([Values(Compression.None, Compression.Gzip, Compression.Lz4)] Compression compression)
        {
            var valueIn = "Hello world!!!";
            var valueInBytes = System.Text.Encoding.UTF8.GetBytes(valueIn);
            var valueStream = new MemoryStream();

            var compressingStream = ValueCompressor.Compress(compression, valueStream);
            compressingStream.Write(valueInBytes, 0, valueInBytes.Length);
            compressingStream.Flush();

            valueStream.Position = 0;
            byte[] valueOut;
            using (var streamOut = ValueCompressor.Decompress(compression, valueStream))
            {
                var buffer = new byte[20];
                var readBytes = streamOut.Read(buffer, 0, buffer.Length);
                valueOut = buffer.Take(readBytes).ToArray();
            }

            CollectionAssert.AreEqual(valueInBytes, valueOut);
        }
    }
}