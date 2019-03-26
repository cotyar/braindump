using System.IO;
using LmdbLight;
using NUnit.Framework;

namespace LmdbLightTest
{
    public class HelperTests
    {

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestConcat()
        {
            var ret = new[] {1, 2, 3}.Concat(new[] {4, 5}, new[] {6, 7, 8}, new[] {9});

            CollectionAssert.AreEqual(new[] {1, 2, 3, 4, 5, 6, 7, 8, 9}, ret);
        }

        [Test]
        public void TestConcatByte()
        {
            var ret = 1.Concat(new[] { 4, 5 }, new[] { 6, 7, 8 }, new[] { 9 });

            CollectionAssert.AreEqual(new[] { 1, 4, 5, 6, 7, 8, 9 }, ret);
        }

        [Test]
        public void TestStartWith()
        {
            var ret = new[] { 1, 2, 3 }.Concat(new[] { 4, 5 }, new[] { 6, 7, 8 }, new[] { 9 });

            Assert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, ret);
        }
    }
}