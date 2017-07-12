using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using CodeElements.NetworkCallTransmissionProtocol.Extensions;
using CodeElements.NetworkCallTransmissionProtocol.Proxy;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    public class EventManager
    {
        private readonly ConcurrentDictionary<ulong, List<EventInterceptor>> _methodCalls;
        private readonly object _methodCallsAddLock = new object();

        public EventManager()
        {
            _methodCalls = new ConcurrentDictionary<ulong, List<EventInterceptor>>();
        }

        public TEventInterface GetEvents<TEventInterface>() where TEventInterface : IEventProvider
        {
            return GetEvents<TEventInterface>(0);
        }

        public TEventInterface GetEvents<TEventInterface>(uint eventSessionId) where TEventInterface : IEventProvider
        {
            var interceptor = new EventInterceptor(eventSessionId, typeof(TEventInterface), this);
            var obj = ProxyFactory.CreateProxy<TEventInterface>(interceptor);
            return obj;
        }

        internal void SubscribeEvent(EventInterceptor eventInterceptor, uint eventId, uint sessionId)
        {
            var key = eventId << 32 | sessionId;

            lock (_methodCallsAddLock)
            {
                if (!_methodCalls.TryGetValue(key, out var eventInterceptors))
                    Debug.Assert(_methodCalls.TryAdd(key, eventInterceptors = new List<EventInterceptor>())); //Assert

                eventInterceptors.Add(eventInterceptor);
            }
        }

        internal void UnsubscribeEvent()
        {

        }
    }

    internal class EventInterceptor : IEventInterceptor
    {
        private readonly uint _eventSessionId;
        private readonly Type _eventInterface;
        private readonly EventManager _eventManager;

        public EventInterceptor(uint eventSessionId, Type eventInterface, EventManager eventManager)
        {
            _eventSessionId = eventSessionId;
            _eventInterface = eventInterface;
            _eventManager = eventManager;
        }

        public void EventSubscribed(EventInfo eventInfo)
        {
            _eventManager.SubscribeEvent(this, eventInfo.GetEventId(_eventInterface), _eventSessionId);
        }

        public void EventUnsubscribed(EventInfo eventInfo)
        {

        }

        public void SuspendSubscribing()
        {
        }

        public void ResumeSubscribing()
        {
        }

        public void Dispose()
        {

        }
    }

    public class EventProvider
    {
        public void SubscribeEvent()
        {

        }

        public void RegisterEvents<TEventInterface>(TEventInterface eventInterface)
        {

        }
    }
}