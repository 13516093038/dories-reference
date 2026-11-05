using System;
using System.Collections.Generic;
using Dories.Reference.Runtime;
using NUnit.Framework;

namespace Dories.Reference.Tests
{
    public class ReferencePoolTests
    {
        private class Foo : IReference
        {
            public int Value;
            public int ClearCount;

            public void Clear()
            {
                Value = 0;
                ClearCount++;
            }
        }

        private ReferencePool _pool;

        [SetUp]
        public void SetUp()
        {
            ReferencePool.ResetStatics();
            _pool = new ReferencePool();
        }

        [TearDown]
        public void TearDown()
        {
            _pool.ClearAll();
            ReferencePool.ResetStatics();
        }

        private ReferenceCollection Collection<T>()
        {
            var list = new List<ReferenceCollection>();
            _pool.GetCollections(list);
            return list.Find(c => c.ReferenceType == typeof(T));
        }

        [Test]
        public void SecondInstance_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new ReferencePool());
        }

        [Test]
        public void Release_ClearsAndReusesInstance()
        {
            var foo = _pool.Acquire<Foo>();
            foo.Value = 42;
            _pool.Release(foo);

            Assert.AreEqual(0, foo.Value);
            Assert.AreEqual(1, foo.ClearCount);
            Assert.AreSame(foo, _pool.Acquire<Foo>());

            var c = Collection<Foo>();
            Assert.AreEqual(2, c.AcquireReferenceCount);
            Assert.AreEqual(1, c.ReleaseReferenceCount);
            Assert.AreEqual(1, c.AddReferenceCount);
            Assert.AreEqual(1, c.UsingReferenceCount);
            Assert.AreEqual(0, c.UnusedReferenceCount);
        }

        [Test]
        public void AcquireByType_ReturnsInstanceOfType()
        {
            Assert.IsInstanceOf<Foo>(_pool.Acquire(typeof(Foo)));
        }

        [Test]
        public void AddRemove_UpdatesUnusedCount()
        {
            _pool.Add<Foo>(5);
            Assert.AreEqual(5, Collection<Foo>().UnusedReferenceCount);

            _pool.Remove<Foo>(2);
            Assert.AreEqual(3, Collection<Foo>().UnusedReferenceCount);

            // 超出剩余数量时只移除剩余的
            _pool.Remove<Foo>(10);
            Assert.AreEqual(0, Collection<Foo>().UnusedReferenceCount);
            Assert.AreEqual(5, Collection<Foo>().RemoveReferenceCount);
        }

        [Test]
        public void ClearAll_RemovesAllCollections()
        {
            _pool.Add<Foo>(3);
            Assert.AreEqual(1, _pool.Count);

            _pool.ClearAll();
            Assert.AreEqual(0, _pool.Count);
        }

        [Test]
        public void StrictCheck_DoubleRelease_Throws()
        {
            _pool.EnableStrictCheck = true;
            var foo = _pool.Acquire<Foo>();
            _pool.Release(foo);

            Assert.Throws<Exception>(() => _pool.Release(foo));
        }

        [Test]
        public void StrictCheck_InvalidType_Throws()
        {
            _pool.EnableStrictCheck = true;

            Assert.Throws<Exception>(() => _pool.Acquire(typeof(string)));
            Assert.Throws<Exception>(() => _pool.Acquire(typeof(IReference)));
        }

        [Test]
        public void Release_Null_Throws()
        {
            Assert.Throws<Exception>(() => _pool.Release(null));
        }
    }
}
