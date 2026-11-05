using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dories.Reference.Runtime;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.TestTools.Constraints;
using Is =UnityEngine.TestTools.Constraints.Is;

namespace Dories.Reference.Tests
{
    /// <summary>
    /// 对比 Inspector 读取引用池数据的几种方式：拷贝到复用 List、直接遍历字典、经接口遍历字典，
    /// 以及无竞争 lock 和每行 ToString（代表绘制开销）的成本。
    /// 结果在 Window/General/Performance Test Report 查看。
    /// </summary>
    [Category("Performance")]
    public class ReferencePoolPerformanceTests
    {
        private class Item<T> : IReference
        {
            public void Clear()
            {
            }
        }

        private readonly List<ReferenceCollection> _collections = new();
        private readonly object _lock = new();
        private ReferencePool _pool;
        private Dictionary<Type, ReferenceCollection> _dictionary;
        private int _sink;

        private void Setup(int typeCount)
        {
            ReferencePool.ResetStatics();
            _pool = new ReferencePool();

            // 用 Item<枚举> 造出 typeCount 种不同的引用类型（void、TypedReference 等值类型不能做泛型实参）
            var args = typeof(int).Assembly.GetTypes()
                .Where(t => t.IsPublic && t.IsEnum)
                .Take(typeCount)
                .ToArray();
            Assert.AreEqual(typeCount, args.Length);
            foreach (var arg in args)
                _pool.Add(typeof(Item<>).MakeGenericType(arg), 4);

            _dictionary = (Dictionary<Type, ReferenceCollection>)typeof(ReferencePool)
                .GetField("_referenceCollections", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(_pool);
        }

        [TearDown]
        public void TearDown()
        {
            _pool?.ClearAll();
            _pool = null;
            ReferencePool.ResetStatics();
        }

        private static int ReadRow(ReferenceCollection c)
        {
            return c.UnusedReferenceCount + c.UsingReferenceCount + c.AcquireReferenceCount
                   + c.ReleaseReferenceCount + c.AddReferenceCount + c.RemoveReferenceCount;
        }

        private void CopyThenRead()
        {
            _pool.GetCollections(_collections);
            foreach (var c in _collections)
                _sink += ReadRow(c);
        }

        private void DirectRead()
        {
            foreach (var pair in _dictionary)
                _sink += ReadRow(pair.Value);
        }

        private void InterfaceRead()
        {
            IReadOnlyDictionary<Type, ReferenceCollection> readOnly = _dictionary;
            foreach (var pair in readOnly)
                _sink += ReadRow(pair.Value);
        }

        private void LockOnly()
        {
            lock (_lock)
            {
                _sink++;
            }
        }

        private void FormatRows()
        {
            foreach (var pair in _dictionary)
            {
                var c = pair.Value;
                _sink += c.UnusedReferenceCount.ToString().Length + c.UsingReferenceCount.ToString().Length
                         + c.AcquireReferenceCount.ToString().Length + c.ReleaseReferenceCount.ToString().Length
                         + c.AddReferenceCount.ToString().Length + c.RemoveReferenceCount.ToString().Length;
            }
        }

        private static void Run(Action action)
        {
            Measure.Method(action)
                .WarmupCount(10)
                .MeasurementCount(30)
                .IterationsPerMeasurement(1000)
                .GC()
                .Run();
        }

        [Test, Performance]
        public void Copy_GetCollections([Values(10, 100)] int typeCount)
        {
            Setup(typeCount);
            Run(CopyThenRead);
        }

        [Test, Performance]
        public void Direct_ForeachDictionary([Values(10, 100)] int typeCount)
        {
            Setup(typeCount);
            Run(DirectRead);
        }

        [Test, Performance]
        public void Interface_ForeachIReadOnlyDictionary([Values(10, 100)] int typeCount)
        {
            Setup(typeCount);
            Run(InterfaceRead);
        }

        [Test, Performance]
        public void Lock_Uncontended()
        {
            Setup(10);
            Run(LockOnly);
        }

        [Test, Performance]
        public void Draw_RowToString([Values(10, 100)] int typeCount)
        {
            Setup(typeCount);
            Run(FormatRows);
        }

        // 下面三个直接断言分配结论；先各调用一次，排除 JIT 首次调用的分配
        [Test]
        public void Copy_DoesNotAllocate()
        {
            Setup(100);
            CopyThenRead();
            Assert.That(CopyThenRead, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Direct_DoesNotAllocate()
        {
            Setup(100);
            DirectRead();
            Assert.That(DirectRead, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Interface_Allocates()
        {
            Setup(100);
            InterfaceRead();
            Assert.That(InterfaceRead, Is.AllocatingGCMemory());
        }
    }
}
