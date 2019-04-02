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
            IClient Create();
        }

        private class LocalClientFactory : IClientFactory
        {
            public IClient Create() => new LmdbLightClient(Config);
        }

        private class GrpcClientFactory : IClientFactory
        {
            public IClient Create() => new GrpcTestClient(Config);
        }


        private const string TestDir = "./client_testdb";

        public static LightningConfig Config =>
            new LightningConfig { Name = TestDir, MaxTables = 20, StorageLimit = 1, WriteBatchMaxDelegates = 100, WriteBatchTimeoutMilliseconds = 1 };

        [SetUp]
        public void Setup()
        {
            if (Directory.Exists(TestDir))
            {
                Directory.Delete(TestDir, true);
            }

            Directory.CreateDirectory(TestDir);
        }

        private IClient CreateClient(Type type) => ((IClientFactory) Activator.CreateInstance(type)).Create();

        [Test]
        public void TestCreateClient([Values(typeof(LocalClientFactory), typeof(GrpcClientFactory))] Type clientFactory)
        {
            using (var client = CreateClient(clientFactory)) { }

            Assert.Pass();
        }

        [Test]
        public void TestAddGet([Values(typeof(LocalClientFactory), typeof(GrpcClientFactory))] Type clientFactory, [Values(1, 10, 100, 1000, 10000)] int iterations)
        {
            using (var client = CreateClient(clientFactory))
            {
                var swTotal = new Stopwatch();
                swTotal.Start();

                var kvs = BuildKeyValuePairs(iterations);

                foreach (var kv in kvs)
                {
                    var testKey = kv.Key;
                    var testValue = kv.Value.Item2;
                    var testValueBytes = kv.Value.Item3;

                    var sw = new Stopwatch();
                    sw.Start();
                    var addRet = client.TryAdd(new[] {testKey}, (k, s) => s.Write(testValueBytes, 0, testValueBytes.Length), DateTimeOffset.UtcNow.AddSeconds(500));
                    sw.Stop();
                    Console.WriteLine($"Write {kv.Value.Item1} elapsed: {sw.Elapsed}");

                    Assert.AreEqual(1, addRet.Count);
                    Assert.AreEqual(testKey, addRet.First());

                    var readBuffer = new byte[10000];
                    int readBytes = -1;
                    sw = new Stopwatch();
                    sw.Start();
                    var getRet = client.TryGet(new[] {testKey}, (k, s) => readBytes = s.Read(readBuffer, 0, readBuffer.Length));
                    Console.WriteLine($"Read {kv.Value.Item1} elapsed: {sw.Elapsed}");

                    Assert.AreEqual(1, getRet.Count);
                    Assert.AreEqual(testKey, getRet.First());
                    Assert.AreEqual(testValueBytes.Length, readBytes);
                    CollectionAssert.AreEqual(testValueBytes, readBuffer.Take(readBytes).ToArray());
                }

                swTotal.Stop();
                Console.WriteLine($"Total te elapsed total: {swTotal.Elapsed}");
            }
        }

        [Test]
        public void TestAddGetMany([Values(typeof(LocalClientFactory), typeof(GrpcClientFactory))] Type clientFactory, [Values(1, 10, 100, 1000, 10000, 100000)] int iterations)
        {
            using (var client = CreateClient(clientFactory))
            {
                //var kvs = Enumerable.Range(0, iterations).ToDictionary(i => Enumerable.Range(0, 20).Aggregate("", (s, _) => s + "test key  ") + i, i =>
                //{
                //    var testValue = Enumerable.Range(0, 20).Aggregate("", (s, _) => s + "test value ") + i;
                //    var testValueBytes = Encoding.UTF8.GetBytes(testValue);
                //    return (testValue, testValueBytes);
                //});

                var kvs = BuildKeyValuePairs(iterations);

                var sw = new Stopwatch();
                sw.Start();
                var addRet = client.TryAdd(kvs.Keys, (k, s) =>
                    {
                        var buffer = kvs[k].Item3;
                        s.Write(buffer, 0, buffer.Length);
                    }, DateTimeOffset.Now.AddSeconds(5));
                sw.Stop();
                Console.WriteLine($"Write elapsed: {sw.Elapsed}");

                Assert.AreEqual(kvs.Count, addRet.Count);
                Assert.IsTrue(addRet.All(k => kvs.ContainsKey(k)));

                var getDict = new Dictionary<string, byte[]>();
                sw = new Stopwatch();
                sw.Start();
                var getRet = client.TryGet(kvs.Keys, (k, s) =>
                {
                    var readBuffer = new byte[100000];
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

        private static Dictionary<string, (int, string, byte[])> BuildKeyValuePairs(int iterations)
        {
            return Enumerable.Range(0, iterations).ToDictionary(i => Guid.NewGuid().ToString(), i =>
            {
                var rnd = new Random();
                var testValue = Enumerable.Range(0, 1024).Aggregate("", (s, _) => s + rnd.Next(16).ToString("X"));
                var testValueBytes = Encoding.UTF8.GetBytes(testValue);
                return (i, testValue, testValueBytes);
            });
        }

        [Test]
        public void TestSearchByPrefix([Values(typeof(LocalClientFactory), typeof(GrpcClientFactory))] Type clientFactory, [Values(1, 10, 100, 1000, 10000, 100000)] int iterations)
        {
            using (var client = CreateClient(clientFactory))
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