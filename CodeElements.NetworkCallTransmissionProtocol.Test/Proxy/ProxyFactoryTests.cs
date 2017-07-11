using System;
using System.Reflection;
using CodeElements.NetworkCallTransmissionProtocol.Internal;
using CodeElements.NetworkCallTransmissionProtocol.Proxy;
using Xunit;

namespace CodeElements.NetworkCallTransmissionProtocol.Test.Proxy
{
    public class ProxyFactoryTests
    {
        [Fact]
        public void TestCreateEventProxySubscribe()
        {
            var interceptor = new TestInterceptor();
            var interfaceObj = ProxyFactory.CreateProxy<IEventTestInterface>(interceptor);
            Assert.False(interceptor.Subscribed);
            interfaceObj.TestEvent3 += (sender, args) => { };
            Assert.True(interceptor.Subscribed);
        }

        [Fact]
        public void TestCreateEventProxyUnsubscribe()
        {
            var interceptor = new TestInterceptor();
            var interfaceObj = ProxyFactory.CreateProxy<IEventTestInterface>(interceptor);

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
        public void TestCreateEventProxyDispose()
        {
            var interceptor = new TestInterceptor();
            var interfaceObj = ProxyFactory.CreateProxy<IEventTestInterface>(interceptor);
            Assert.False(interceptor.Disposed);
            interfaceObj.Dispose();
            Assert.True(interceptor.Disposed);
        }

        [Fact]
        public void TestCreateEventProxyRaise()
        {
            var interceptor = new TestInterceptor();
            var interfaceObj = ProxyFactory.CreateProxy<IEventTestInterface>(interceptor);
            var raised = false;
            interfaceObj.TestEvent3 += (sender, s) => raised = true;

            var method = interfaceObj.GetType().GetMethod("OnTestEvent3");
            method.Invoke(interfaceObj, new[] {"asd"});
            Assert.True(raised);
        }

        private class TestInterceptor : IEventInterceptor
        {
            public bool Subscribed { get; set; }
            public bool Disposed { get; set; }
            public bool Unsubscribed { get; set; }

            public void EventSubscribed(EventInfo eventInfo)
            {
                Subscribed = true;
            }

            public void EventUnsubscribed(EventInfo eventInfo)
            {
                Unsubscribed = true;
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }

    public interface IEventTestInterface : IDisposable
    {
        event EventHandler TestEvent;
        event ResolveEventHandler TestEvent2;
        event EventHandler<string> TestEvent3;
    }
}