using System;
using System.Threading.Tasks;
using Xunit;

namespace CodeElements.NetworkCallTransmissionProtocol.Test
{
    public class ExecuterInterfaceCacheTests
    {
        private void TestBuildCache<TInterface>()
        {
            var cache = ExecuterInterfaceCache.Build<TInterface>();
            Assert.True(cache.MethodInvokers.Count > 0);
        }

        [Fact]
        public void TestInvalidInterfaceWithProperty()
        {
            Assert.Throws<ArgumentException>(() => TestBuildCache<IInvalidInterface1>());
        }

        [Fact]
        public void TestInvalidInterfaceWithVoid()
        {
            Assert.Throws<ArgumentException>(() => TestBuildCache<IInvalidInterface2>());
        }

        [Fact]
        public void TestInvalidInterfaceEmpty()
        {
            Assert.Throws<ArgumentException>(() => TestBuildCache<IInvalidInterface3>());
        }

        [Fact]
        public void TestValidInterface()
        {
            TestBuildCache<IValidInterface>();
        }

        private interface IInvalidInterface1
        {
            Task Test1();
            string Test2 { get; }
        }

        private interface IInvalidInterface2
        {
            Task Test1();
            void Test2(bool asd);
        }

        private interface IInvalidInterface3
        {
        }

        private interface IValidInterface
        {
            Task Test(string asd);
            Task<bool> Test2(string asd);
        }
    }
}