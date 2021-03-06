﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using CodeElements.NetworkCall.Proxy;
using Xunit;

namespace CodeElements.NetworkCall.Test.Proxy
{
    public class ProxyFactoryTests
    {
        private T CreateEvents<T>(IEventInterceptor interceptor)
        {
            var proxyBuilder = new ProxyFactoryBuilder(typeof(T));
            proxyBuilder.InterceptEvents();
            return proxyBuilder.Build().Create<T>(asyncInterceptor: null, eventInterceptor: interceptor);
        }

        private T CreateMethods<T>(IAsyncInterceptor interceptor)
        {
            var proxyBuilder = new ProxyFactoryBuilder(typeof(T));
            proxyBuilder.InterceptMethods();
            return proxyBuilder.Build().Create<T>(interceptor, eventInterceptor: null);
        }

        [Fact]
        public void TestCreateEventProxySubscribe()
        {
            var interceptor = new TestInterceptor();

            var interfaceObj = CreateEvents<IEventTestInterface>(interceptor);
            Assert.False(interceptor.Subscribed);
            interfaceObj.TestEvent3 += (sender, args) => { };
            Assert.True(interceptor.Subscribed);
        }

        [Fact]
        public void TestCreateEventProxyUnsubscribe()
        {
            var interceptor = new TestInterceptor();
            var interfaceObj = CreateEvents<IEventTestInterface>(interceptor);

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
            var interfaceObj = CreateEvents<IEventTestInterface>(interceptor);
            var raised = false;
            interfaceObj.TestEvent += (s, p) =>
            {
                Assert.Equal(23, p);
                raised = true;
            };

            var raiser = (IEventInterceptorProxy) interfaceObj;
            raiser.TriggerEvent(0, 23);
            Assert.True(raised);
        }

        [Fact]
        public void TestCreateNonGenericEventProxyRaise()
        {
            var interceptor = new TestInterceptor();
            var interfaceObj = CreateEvents<IEventTestInterface>(interceptor);
            var raised = false;
            interfaceObj.TestEvent2 += (sender, args) =>
            {
                Assert.Equal(EventArgs.Empty, args);
                raised = true;
            };

            var raiser = (IEventInterceptorProxy) interfaceObj;
            raiser.TriggerEvent(1, null);
            Assert.True(raised);
        }

        [Fact]
        public void TestCreateEventProxyRaiseWithParameter()
        {
            var interceptor = new TestInterceptor();
            var interfaceObj = CreateEvents<IEventTestInterface>(interceptor);
            var raised = false;
            interfaceObj.TestEvent3 += (sender, s) => raised = true;

            var raiser = (IEventInterceptorProxy) interfaceObj;
            raiser.TriggerEvent(2, "asd");
            Assert.True(raised);
        }

        [Fact]
        public void TestCreateInterface()
        {
            var interceptor = new TestInceptor();
            var obj = CreateMethods<ISimpleInterface>(interceptor);
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
        event EventHandler<int> TestEvent;
        event EventHandler TestEvent2;
        event EventHandler<string> TestEvent3;
    }
}