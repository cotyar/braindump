using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using LmdbCache.Domain;
using LmdbCacheServer;
using LmdbCacheServer.Replica;
using LmdbLight;
using NUnit.Framework;

namespace LmdbLightTest
{
    public class ClientTests
    {
        private interface IClientFactory
        {
            IClient Create(LightningConfig config);
        }

        private class LocalClientFactory : IClientFactory
        {
            public IClient Create(LightningConfig config) => new LmdbLightClient(config);
        }

        private class GrpcClientFactory : IClientFactory
        {
            public IClient Create(LightningConfig config) => new GrpcTestClient(config);
        }


        private const string TestDir = "./client_testdb";
//        private const string TestDir = @"D:\CacheTest\client_testdb";

        public static LightningConfig Config =>
            new LightningConfig
            {
                Name = TestDir,
                MaxTables = 20,
                StorageLimit = 10,
                WriteBatchMaxDelegates = 100,
                WriteBatchTimeoutMilliseconds = 1,
                AsyncStore = false
            };

        [SetUp]
        public void Setup()
        {
            if (Directory.Exists(TestDir))
            {
                Directory.Delete(TestDir, true);
            }

            Directory.CreateDirectory(TestDir);
        }

        private IClient CreateClient(Type type, LightningConfig config) => ((IClientFactory) Activator.CreateInstance(type)).Create(config);

        [Test]
        public void TestCreateClient([Values(typeof(LocalClientFactory), typeof(GrpcClientFactory))] Type clientFactory)
        {
            using (var client = CreateClient(clientFactory, Config)) { }

            Assert.Pass();
        }

        [Test]
        public void TestAddGet([Values(typeof(LocalClientFactory), typeof(GrpcClientFactory))] Type clientFactory, [Values(1, 10, 100, 1000, 10000)] int iterations)
        {
            using (var client = CreateClient(clientFactory, Config))
            {
                var totalWrite = new TimeSpan();
                var totalRead = new TimeSpan();

                var swTotal = new Stopwatch();
                swTotal.Start();
                var kvs = BuildKeyValuePairs(iterations, 1024);
                swTotal.Stop();
                Console.WriteLine($"Preparing data elapsed: {swTotal.Elapsed}");

                swTotal = new Stopwatch();
                swTotal.Start();

                foreach (var kv in kvs)
                {
                    var testKey = kv.Key;
                    var testValue = kv.Value.Item2;

                    var sw = new Stopwatch();
                    sw.Start();
                    var addRet = client.TryAdd(new[] {testKey}, (k, s) => s.Write(testValue, 0, testValue.Length), DateTimeOffset.UtcNow.AddSeconds(500));
                    sw.Stop();
//                    Console.WriteLine($"Write {kv.Value.Item1} elapsed: {sw.Elapsed}");
                    totalWrite += sw.Elapsed;

                    Assert.AreEqual(1, addRet.Count);
                    Assert.AreEqual(testKey, addRet.First());

                    var readBuffer = new byte[1000000];
                    int readBytes = -1;
                    sw = new Stopwatch();
                    sw.Start();
                    var getRet = client.TryGet(new[] {testKey}, (k, s) => readBytes = s.Read(readBuffer, 0, readBuffer.Length));
                    sw.Stop();
//                    Console.WriteLine($"Read {kv.Value.Item1} elapsed: {sw.Elapsed}");
                    totalRead += sw.Elapsed;

                    Assert.AreEqual(1, getRet.Count);
                    Assert.AreEqual(testKey, getRet.First());
                    Assert.AreEqual(testValue.Length, readBytes);
                    CollectionAssert.AreEqual(testValue, readBuffer.Take(readBytes).ToArray());
                }

                swTotal.Stop();
                Console.WriteLine($"Elapsed total: {swTotal.Elapsed}");
                Console.WriteLine($"Elapsed total Write: {totalWrite}");
                Console.WriteLine($"Elapsed total Read: {totalRead}");
            }
        }

        [Test]
        public void TestAddGetMany([Values(typeof(LocalClientFactory), typeof(GrpcClientFactory))] Type clientFactory, [Values(1, 10, 100, 1000, 10000, 100000)] int iterations)
        {
            using (var client = CreateClient(clientFactory, Config))
            {
                //var kvs = Enumerable.Range(0, iterations).ToDictionary(i => Enumerable.Range(0, 20).Aggregate("", (s, _) => s + "test key  ") + i, i =>
                //{
                //    var testValue = Enumerable.Range(0, 20).Aggregate("", (s, _) => s + "test value ") + i;
                //    var testValueBytes = Encoding.UTF8.GetBytes(testValue);
                //    return (testValue, testValueBytes);
                //});

                var sw = new Stopwatch();
                sw.Start();
                var kvs = BuildKeyValuePairs(iterations, 1024);
                sw.Stop();
                Console.WriteLine($"Preparing data elapsed: {sw.Elapsed}");

                sw = new Stopwatch();
                sw.Start();
                var addRet = client.TryAdd(kvs.Keys, (k, s) =>
                    {
                        var buffer = kvs[k].Item2;
                        s.Write(buffer, 0, buffer.Length);
                    }, DateTimeOffset.Now.AddSeconds(50));
                sw.Stop();
                Console.WriteLine($"Write elapsed: {sw.Elapsed}");

                Assert.AreEqual(kvs.Count, addRet.Count);
                Assert.IsTrue(addRet.All(k => kvs.ContainsKey(k)));

                var getDict = new Dictionary<string, byte[]>();
                sw = new Stopwatch();
                sw.Start();
                var getRet = client.TryGet(kvs.Keys, (k, s) =>
                {
                    var readBuffer = new byte[1000000];
                    var readBytes = s.Read(readBuffer, 0, readBuffer.Length);
                    getDict[k] = readBuffer.Take(readBytes).ToArray();
                });
                Console.WriteLine($"Read elapsed: {sw.Elapsed}");

                Assert.AreEqual(kvs.Keys.Count, getRet.Count);
                Assert.IsTrue(getRet.All(k => kvs.ContainsKey(k)));
                Assert.AreEqual(kvs.Keys.Count, getDict.Count);
                foreach (var kv in getDict)
                {
                    Assert.IsTrue(kvs.ContainsKey(kv.Key));
                    CollectionAssert.AreEqual(kvs[kv.Key].Item2, kv.Value);
                }
            }
        }

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

        [Test]
        public void TestSearchByPrefix([Values(typeof(LocalClientFactory), typeof(GrpcClientFactory))] Type clientFactory, [Values(1, 10, 100, 1000, 10000, 100000)] int iterations)
        {
            using (var client = CreateClient(clientFactory, Config))
            {
                var kvs = Enumerable.Range(0, iterations).ToDictionary(i => Enumerable.Range(0, 20).Aggregate("", (s, _) => s + "test key  ") + i, i =>
                {
                    var testValue = Enumerable.Range(0, 20).Aggregate("", (s, _) => s + "test value ") + i;
                    var testValueBytes = Encoding.UTF8.GetBytes(testValue);
                    return (testValue, testValueBytes);
                });

                var addRet = client.TryAdd(kvs.Keys, (k, s) =>
                {
                    var buffer = kvs[k].Item2;
                    s.Write(buffer, 0, buffer.Length);
                }, DateTimeOffset.Now.AddSeconds(5));

                var sw = new Stopwatch();
                sw.Start();
                var getRet = client.SearchByPrefix("test");
                Console.WriteLine($"Search elapsed: '{sw.Elapsed}', #records returned: '{getRet.Count}'");

                Assert.AreEqual(kvs.Keys.Count, getRet.Count);
                Assert.IsTrue(getRet.All(k => kvs.ContainsKey(k)));
            }
        }
    }
}