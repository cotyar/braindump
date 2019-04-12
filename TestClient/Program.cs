using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using LmdbCache;
using LmdbCacheClient;

namespace TestClient
{
    class Program
    {
        private static Dictionary<string, (int, byte[])> BuildKeyValuePairs(int iterations, int length)
        {
            var rnd = new Random();
            return Enumerable.Range(0, iterations).ToDictionary(i => Guid.NewGuid().ToString(), i =>
            {
                var testValue = new byte[length];
                rnd.NextBytes(testValue);
                return (i, testValue);
            });
        }


        public static void TestAddGetManyParallel(
            string host,
            uint port,
            int writeStreams,
            int readStreams,
            int keysCount)
        {
            var valueLength = 1024;

            var sw = new Stopwatch();
            sw.Start();
            var kvsAll = BuildKeyValuePairs(keysCount * writeStreams, valueLength);
            sw.Stop();
            Console.WriteLine($"Preparing data elapsed: {sw.Elapsed}");

            var swAll = new Stopwatch();
            swAll.Start();
            Parallel.For(0, writeStreams, new ParallelOptions { MaxDegreeOfParallelism = writeStreams }, i =>
            {
                var kvs = kvsAll.Skip(i * keysCount).Take(keysCount).ToDictionary(kv => kv.Key, kv => kv.Value);

                using (var writeClient = new LightClient(new Channel($"{host}:{port}", ChannelCredentials.Insecure), true))
                {
                    sw = new Stopwatch();
                    sw.Start();
                    var addRet = writeClient.TryAdd(kvs.Keys, (k, s) =>
                    {
                        var buffer = kvs[k].Item2;
                        s.Write(buffer, 0, buffer.Length);
                    }, DateTimeOffset.Now.AddSeconds(500));
                    sw.Stop();
                    Console.WriteLine($"Write elapsed: {sw.Elapsed}");
                }
            });
            swAll.Stop();
            Console.WriteLine($"Write Elapsed total: {swAll.Elapsed}");
            Console.WriteLine($"Write Elapsed per thread: {swAll.Elapsed / writeStreams}");


            swAll = new Stopwatch();
            swAll.Start();
            Parallel.For(0, readStreams, new ParallelOptions { MaxDegreeOfParallelism = readStreams }, i =>
            {
                var kvs = kvsAll.Skip((i % writeStreams) * keysCount).Take(keysCount).ToDictionary(kv => kv.Key, kv => kv.Value);

                using (var readClient = new LightClient(new Channel($"{host}:{port}", ChannelCredentials.Insecure), true))
                {
                    var getDict = new Dictionary<string, byte[]>();
                    sw = new Stopwatch();
                    sw.Start();
                    var getRet = readClient.TryGet(kvs.Keys, (k, s) =>
                    {
                        var readBuffer = new byte[valueLength];
                        var readBytes = s.Read(readBuffer, 0, readBuffer.Length);
                        getDict[k] = readBuffer.Take(readBytes).ToArray();
                    });
                    sw.Stop();
                    Console.WriteLine($"Read elapsed: {sw.Elapsed}");

                }
            });
            swAll.Stop();
            Console.WriteLine($"Read Elapsed total: {swAll.Elapsed}");
            Console.WriteLine($"Read Elapsed per thread: {swAll.Elapsed / readStreams}");
        }


        static void Main(string[] args)
        {
            TestAddGetManyParallel("127.0.0.1", 40051, 20, 20, 10000);

            Console.ReadLine();
        }
    }
}
