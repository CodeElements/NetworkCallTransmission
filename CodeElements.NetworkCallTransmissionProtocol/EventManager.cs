using System;
using System.Reflection;
using CodeElements.NetworkCallTransmissionProtocol.Proxy;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    public class EventManager
    {
        public TEventInterface GetEvents<TEventInterface>(Guid guid) where TEventInterface : IDisposable
        {
            var interceptor = new EventInterceptor(guid, typeof(TEventInterface));
            var obj = ProxyFactory.CreateProxy<TEventInterface>(interceptor);
            return obj;
        }

        internal void SubscribeEvent()
        {

        }
    }

    internal class EventInterceptor : IEventInterceptor
    {
        public EventInterceptor(Guid guid, Type eventInterface)
        {

        }

        public void EventSubscribed(EventInfo eventInfo)
        {

        }

        public void EventUnsubscribed(EventInfo eventInfo)
        {

        }

        public void Dispose()
        {

        }
    }

    public class EventProvider
    {
        public void RegisterEvents<TEventInterface>(TEventInterface eventInterface)
        {

        }
    }
}