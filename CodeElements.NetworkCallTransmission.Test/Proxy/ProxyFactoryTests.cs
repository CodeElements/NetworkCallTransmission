using System.Reflection;
using System.Threading.Tasks;
using CodeElements.NetworkCallTransmission.Proxy;
using Xunit;

namespace CodeElements.NetworkCallTransmission.Test.Proxy
{
    public class ProxyFactoryTests
    {
        [Fact]
        public void TestCreateEventProxySubscribe()
        {
            var interceptor = new TestInterceptor();
            var interfaceObj =
                ProxyFactory.InitializeEventProxy<IEventTestInterface>(ProxyFactory.CreateProxy<IEventTestInterface>(), interceptor);
            Assert.False(interceptor.Subscribed);
            interfaceObj.TestEvent3 += (sender, args) => { };
            Assert.True(interceptor.Subscribed);
        }

        [Fact]
        public void TestCreateEventProxyUnsubscribe()
        {
            var interceptor = new TestInterceptor();
            var interfaceObj =
                ProxyFactory.InitializeEventProxy<IEventTestInterface>(ProxyFactory.CreateProxy<IEventTestInterface>(), interceptor);

            interfaceObj.TestEvent3 += Handler;
            Assert.True(interceptor.Subscribed);

            Assert.False(interceptor.Unsubscribed);
            interfaceObj.TestEvent3 -= Handler;
            Assert.True(interceptor.Unsubscribed);

            void Handler(object sender, string args)
            {
            }
        }

        [Fact]
        public void TestCreateEventProxyRaise()
        {
            var interceptor = new TestInterceptor();
            var interfaceObj =
                ProxyFactory.InitializeEventProxy<IEventTestInterface>(ProxyFactory.CreateProxy<IEventTestInterface>(), interceptor);
            var raised = false;
            interfaceObj.TestEvent += info =>
            {
                Assert.Equal(23, info);
                raised = true;
            };

            var raiser = (IEventInterceptorProxy) interfaceObj;
            raiser.TriggerEvent(0, 23, null);
            Assert.True(raised);
        }

        [Fact]
        public void TestCreateEventProxyRaiseWithParameter()
        {
            var interceptor = new TestInterceptor();
            var interfaceObj =
                ProxyFactory.InitializeEventProxy<IEventTestInterface>(ProxyFactory.CreateProxy<IEventTestInterface>(), interceptor);
            var raised = false;
            interfaceObj.TestEvent3 += (sender, s) => raised = true;

            var raiser = (IEventInterceptorProxy) interfaceObj;
            raiser.TriggerEvent(1, TransmissionInfo.Empty, "asd");
            Assert.True(raised);
        }

        [Fact]
        public void TestCreateInterface()
        {
            var interceptor = new TestInceptor();
            var obj = ProxyFactory.CreateProxy<ISimpleInterface>(interceptor);
            obj.Test();
            Assert.True(interceptor.MethodInvoked);
        }

        private class TestInterceptor : IEventInterceptor
        {
            public bool Subscribed { get; set; }
            public bool Disposed { get; set; }
            public bool Unsubscribed { get; set; }
            public bool Suspended { get; set; }
            public bool Resumed { get; set; }

            public void EventSubscribed(EventInfo eventInfo)
            {
                Subscribed = true;
            }

            public void EventUnsubscribed(EventInfo eventInfo)
            {
                Unsubscribed = true;
            }

            public void SuspendSubscribing()
            {
                Suspended = true;
            }

            public void ResumeSubscribing()
            {
                Resumed = true;
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }

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
    }

    public interface ISimpleInterface
    {
        Task Test();
    }

    public interface IEventTestInterface
    {
        event TransmittedEventHandler<int> TestEvent;
        //event ResolveEventHandler TestEvent2;
        event TransmittedEventHandler<TransmissionInfo, string> TestEvent3;
    }
}