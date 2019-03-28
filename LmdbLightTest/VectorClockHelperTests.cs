using System;
using System.Linq;
using LmdbCache;
using LmdbCacheServer;
using NUnit.Framework;

namespace LmdbLightTest
{
    public class VectorClockHelperTests
    {

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestCreateVectorClock()
        {
            var vc = VectorClockHelper.CreateEmpty();

            Assert.IsNotNull(vc);
            Assert.AreEqual(0, vc.Replicas.Count);
        }

        [Test]
        public void TestSetAndGet()
        {
            var value = 10UL;
            var replicaId = "replica 1";
            var vc = VectorClockHelper.CreateEmpty().SetReplicaValue(replicaId, value);
            var actualValue = vc.GetReplicaValue(replicaId);

            Assert.IsTrue(actualValue.HasValue);
            Assert.AreEqual(value, actualValue.Value);
        }

        [Test]
        public void TestSetAndGetRandom([Values(1, 2, 5, 10, 20, 100)] int replicaCount)
        {
            var repValues = GenerateRandomKeyValues(replicaCount);

            var vc = VectorClockHelper.CreateEmpty();
            vc = vc.SetReplicaValues(repValues);

            CheckContainsAll(repValues, vc);
        }

        private static void CheckContainsAll((string, ulong)[] repValues, VectorClock vc)
        {
            foreach (var repValue in repValues)
            {
                Assert.AreEqual((ulong?) repValue.Item2, vc.GetReplicaValue(repValue.Item1));
            }
        }

        [Test]
        public void TestSetAndGetRandomSeq([Values(1, 2, 5, 10, 20, 100)] int replicaCount)
        {
            var repValues = GenerateRandomKeyValues(replicaCount);

            var vc = VectorClockHelper.CreateEmpty();
            vc = repValues.Aggregate(vc, (vClock, kv) => vClock.SetReplicaValue(kv.Item1, kv.Item2));

            CheckContainsAll(repValues, vc);
        }

        private static (string, ulong)[] GenerateRandomKeyValues(int replicaCount)
        {
            var rnd = new Random();
            var repValues = Enumerable.Range(0, replicaCount).Select(i => ((ulong) i * 10000) + ((ulong) rnd.Next() / 10000))
                . // Formula here is just to avoid overlaps
                Select(v => ($"rep key '{v}'", v)).ToArray();
            return repValues;
        }

        [Test]
        public void TestCompareEqualLeftRightRandom([Values(1, 2, 5, 10, 20, 100)] int replicaCount)
        {
            var repValues = GenerateRandomKeyValues(replicaCount);

            var vc = VectorClockHelper.CreateEmpty();
            var vc1 = vc.SetReplicaValues(repValues);
            var vc2 = vc1.Clone();

            Assert.AreEqual(Ord.Eq, vc1.Compare(vc2));
            Assert.AreEqual(Ord.Eq, vc1.SetReplicaValue("Breaking value", 0).Compare(vc2));
            Assert.AreEqual(Ord.Eq, vc1.Compare(vc2.SetReplicaValue("Breaking value", 0)));

            Assert.AreEqual(Ord.Lt, vc.Compare(vc2));
            Assert.AreEqual(Ord.Gt, vc2.Compare(vc));

            Assert.AreEqual(Ord.Cc, vc.SetReplicaValue("Breaking value", 1).Compare(vc2));
            Assert.AreEqual(Ord.Cc, vc1.SetReplicaValue("Breaking value 1", 1).Compare(vc2.SetReplicaValue("Breaking value 2", 1)));

            Assert.AreEqual(Ord.Lt, vc1.Compare(vc2.SetTime(DateTimeOffset.UtcNow.ToTimestamp())));
            Assert.AreEqual(Ord.Gt, vc1.SetTime(DateTimeOffset.UtcNow.ToTimestamp()).Compare(vc2));
        }

        [Test]
        public void TestMerge([Values(1, 2, 5, 10, 20, 100)] int replicaCount)
        {
            var vc = VectorClockHelper.CreateEmpty();

            var repValues1 = GenerateRandomKeyValues(replicaCount);
            var vc1 = vc.SetReplicaValues(repValues1);

            var repValues2 = GenerateRandomKeyValues(replicaCount + 2);
            var vc2 = vc.SetReplicaValues(repValues2);

            var vcMerged1 = vc1.Merge(vc2);
            var vcMerged2 = vc2.Merge(vc1);

            Assert.LessOrEqual(repValues1.Length, vcMerged1.Replicas.Count);
            Assert.LessOrEqual(repValues2.Length, vcMerged2.Replicas.Count);
            Assert.AreEqual(Ord.Eq, vcMerged1.Compare(vcMerged2));
        }
    }
}