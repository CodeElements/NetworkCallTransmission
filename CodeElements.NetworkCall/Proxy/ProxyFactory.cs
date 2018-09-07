using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CodeElements.NetworkCall.Proxy
{
    internal class ProxyFactory
    {
        private readonly TypeInfo _proxyImplementation;

        public ProxyFactory(TypeInfo proxyImplementation, IReadOnlyList<MethodInfo> interceptedMethods,
            IReadOnlyList<EventInfo> interceptedEvents)
        {
            _proxyImplementation = proxyImplementation;
            InterceptedMethods = interceptedMethods;
            InterceptedEvents = interceptedEvents;
        }

        public IReadOnlyList<MethodInfo> InterceptedMethods { get; }
        public IReadOnlyList<EventInfo> InterceptedEvents { get; }

        public object Create(IAsyncInterceptor asyncInterceptor, IEventInterceptor eventInterceptor)
        {
            var result = Activator.CreateInstance(_proxyImplementation);

            if (InterceptedMethods != null)
            {
                var asyncProxy = (IAsyncInterceptorProxy)result;
                asyncProxy.Interceptor = asyncInterceptor ?? throw new ArgumentNullException(nameof(asyncInterceptor));
                asyncProxy.Methods = InterceptedMethods.ToArray();
            }

            if (InterceptedEvents != null)
            {
                var eventProxy = (IEventInterceptorProxy)result;
                eventProxy.Interceptor = eventInterceptor ?? throw new ArgumentNullException(nameof(asyncInterceptor));
                eventProxy.Events = InterceptedEvents.ToArray();
            }

            return result;
        }

        public T Create<T>(IAsyncInterceptor asyncInterceptor, IEventInterceptor eventInterceptor) =>
            (T) Create(asyncInterceptor, eventInterceptor);
    }
}