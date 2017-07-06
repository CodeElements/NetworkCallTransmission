using System.Threading.Tasks;
using CodeElements.NetworkCallTransmissionProtocol.Proxy;
using Xunit;

namespace CodeElements.NetworkCallTransmissionProtocol.Test
{
    public class ProxyFactoryTests
    {
        private class TestInceptor : IAsyncInterceptor
        {
            public bool MethodInvoked { get; private set; }

            public void InterceptAsynchronous(IInvocation invocation)
            {
                MethodInvoked = true;
            }

            public void InterceptAsynchronous<TResult>(IInvocation invocation)
            {
                MethodInvoked = true;
            }
        }

        [Fact]
        public void TestCreateInterface()
        {
            var interceptor = new TestInceptor();
            var obj = ProxyFactory.CreateProxy<ISimpleInterface>(interceptor);
            obj.Test();
            Assert.True(interceptor.MethodInvoked);
        }
    }

    public interface ISimpleInterface
    {
        Task Test();
    }
}