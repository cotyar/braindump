using System.IO;
using LmdbCache;
using LmdbLight;
using NUnit.Framework;

namespace LmdbLightTest
{
    public class LightningPersistenceTests
    {
        private const string TestDir = "./testdb";

        private LightningConfig _config;

        [SetUp]
        public void Setup()
        {
            if (Directory.Exists(TestDir))
            {
                Directory.Delete(TestDir, true);
            }

            Directory.CreateDirectory(TestDir);

            _config = new LightningConfig { Name = TestDir, MaxTables = 20, StorageLimit = 1, WriteBatchMaxDelegates = 100, WriteBatchTimeoutMilliseconds = 1000 };
        }

        [Test]
        public void TestCreateDb()
        {
            using (var persistence = new LightningPersistence(_config)) { }

            Assert.Pass();
        }

        [Test]
        public void TestCreateTable()
        {
            using (var persistence = new LightningPersistence(_config))
            {
                using (var table = persistence.OpenTable("testtable"))
                {
                    
                }
            }

            Assert.Pass();
        }

        //[Test]
        //public void TestAddOne()
        //{
        //    var testKey = "test key";
        //    var testValue = "test value";

        //    using (var persistence = new LightningPersistence(_config))
        //    {
        //        persistence.AddBatch(persistence.)
        //    }

        //    Assert.Pass();
        //}
    }
}