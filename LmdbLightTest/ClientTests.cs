using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using LmdbLight;
using NUnit.Framework;

namespace LmdbLightTest
{
    public class ClientTests
    {
        private const string TestDir = "./client_testdb";

        private LightningConfig _config;

        [SetUp]
        public void Setup()
        {
            if (Directory.Exists(TestDir))
            {
                Directory.Delete(TestDir, true);
            }

            Directory.CreateDirectory(TestDir);

            _config = new LightningConfig { Name = TestDir, MaxTables = 20, StorageLimit = 1, WriteBatchMaxDelegates = 100, WriteBatchTimeoutMilliseconds = 1 };
        }

        [Test]
        public void TestCreateClient()
        {
            using (var client = new LmdbLightClient(_config)) { }

            Assert.Pass();
        }

        [Test]
        public void TestAddGet([Values(1, 10, 100, 1000, 10000)] int iterations)
        {
            using (var client = new LmdbLightClient(_config))
            {
                for (int i = 0; i < iterations; i++)
                {
                    var testKey = "test key " + i;
                    var testValue = "test value" + i;
                    var testValueBytes = Encoding.UTF8.GetBytes(testValue);

                    var sw = new Stopwatch();
                    sw.Start();
                    var addRet = client.TryAdd(new[] {testKey}, (k, s) => s.Write(testValueBytes, 0, testValueBytes.Length), DateTimeOffset.Now.AddSeconds(5));
                    sw.Stop();
                    Console.WriteLine($"Write {i} elapsed: {sw.Elapsed}");

                    Assert.AreEqual(1, addRet.Count);
                    Assert.AreEqual(testKey, addRet.First());

                    var readBuffer = new byte[100];
                    int readBytes = -1;
                    sw = new Stopwatch();
                    sw.Start();
                    var getRet = client.TryGet(new[] {testKey}, (k, s) => readBytes = s.Read(readBuffer, 0, readBuffer.Length));
                    Console.WriteLine($"Read {i} elapsed: {sw.Elapsed}");

                    Assert.AreEqual(1, getRet.Count);
                    Assert.AreEqual(testKey, getRet.First());
                    Assert.AreEqual(testValueBytes.Length, readBytes);
                    CollectionAssert.AreEqual(testValueBytes, readBuffer.Take(readBytes).ToArray());
                }
            }
        }

        [Test]
        public void TestAddGetMany([Values(1, 10, 100, 1000, 10000, 100000)] int iterations)
        {
            using (var client = new LmdbLightClient(_config))
            {
                var kvs = Enumerable.Range(0, iterations).ToDictionary(i => Enumerable.Range(0, 20).Aggregate("", (s, _) => s + "test key  ") + i, i =>
                {
                    var testValue = Enumerable.Range(0, 20).Aggregate("", (s, _) => s + "test value ") + i;
                    var testValueBytes = Encoding.UTF8.GetBytes(testValue);
                    return (testValue, testValueBytes);
                });

                var sw = new Stopwatch();
                sw.Start();
                var addRet = client.TryAdd(kvs.Keys, (k, s) =>
                    {
                        var buffer = kvs[k].Item2;
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
                    var readBuffer = new byte[1000];
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

        [Test]
        public void TestSearchByPrefix([Values(1, 10, 100, 1000, 10000, 100000)] int iterations)
        {
            using (var client = new LmdbLightClient(_config))
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
                Console.WriteLine($"Search elapsed: {sw.Elapsed}");

                Assert.AreEqual(kvs.Keys.Count, getRet.Count);
                Assert.IsTrue(getRet.All(k => kvs.ContainsKey(k)));
            }
        }
    }
}