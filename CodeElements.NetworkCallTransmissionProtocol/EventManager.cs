using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CodeElements.NetworkCallTransmissionProtocol.Extensions;
using CodeElements.NetworkCallTransmissionProtocol.Internal;
using CodeElements.NetworkCallTransmissionProtocol.Proxy;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    public class EventManager : DataTransmitter
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

        internal void SubscribeEvents(EventInterceptor eventInterceptor, IEnumerable<ulong> events)
        {
            var eventsToRegister = new List<ulong>();

            lock (_methodCallsAddLock)
            {
                foreach (var eventKey in events)
                {
                    if (!_methodCalls.TryGetValue(eventKey, out var eventInterceptors))
                    {
                        Debug.Assert(_methodCalls.TryAdd(eventKey,
                            eventInterceptors = new List<EventInterceptor>())); //Assert
                        eventsToRegister.Add(eventKey);
                    }

                    eventInterceptors.Add(eventInterceptor);
                }
            }

            if (eventsToRegister.Count > 0)
                SendEventAction(true, eventsToRegister);
        }

        internal void UnsubscribeEvents(EventInterceptor eventInterceptor, IEnumerable<ulong> events)
        {
            var eventsToUnregister = new List<ulong>();

            lock (_methodCallsAddLock)
            {
                foreach (var eventKey in events)
                {
                    if (_methodCalls.TryGetValue(eventKey, out var eventInterceptors))
                    {
                        eventInterceptors.Remove(eventInterceptor);
                        if (eventInterceptors.Count == 0)
                        {
                            eventsToUnregister.Add(eventKey);
                            _methodCalls.TryRemove(eventKey, out var _);
                        }
                    }
                }
            }

            if (eventsToUnregister.Count > 0)
                SendEventAction(false, eventsToUnregister);
        }

        private void SendEventAction(bool subscribe, IList<ulong> events)
        {
            var package = new byte[CustomOffset + 1 /* prefix */ + 2 /* amount of events */ + events.Count * 8];
            package[CustomOffset] = (byte) (subscribe
                ? EventPackageType.SubscribeEvent
                : EventPackageType.UnsubscribeEvent);

            Buffer.BlockCopy(BitConverter.GetBytes((ushort) events.Count), 0, package, CustomOffset + 1,
                2);
            for (var index = 0; index < events.Count; index++)
            {
                var eventId = events[index];
                Buffer.BlockCopy(BitConverter.GetBytes(eventId), 0, package, CustomOffset + 3 + index * 8, 8);
            }

            OnSendData(new ResponseData(package));
        }
    }

    internal class EventInterceptor : IEventInterceptor
    {
        private readonly uint _eventSessionId;
        private readonly Type _eventInterface;
        private readonly EventManager _eventManager;
        private bool _isSuspended;
        private readonly Queue<EventInfo> _waitingEvents;
        private readonly Dictionary<EventInfo, int> _subscribedEvents;
        private readonly object _eventSubscribingLock = new object();
        private readonly object _suspendingLock = new object();

        public EventInterceptor(uint eventSessionId, Type eventInterface, EventManager eventManager)
        {
            _eventSessionId = eventSessionId;
            _eventInterface = eventInterface;
            _eventManager = eventManager;
            _waitingEvents = new Queue<EventInfo>();
            _subscribedEvents = new Dictionary<EventInfo, int>();
        }

        public void EventSubscribed(EventInfo eventInfo)
        {
            //it is no problem if an event is instantly subscribed even if it should be suspended but it is a problem
            //if an event is delayed subscribed if it was disabled (because ResumeSubscribing wont be called anymore)
            if (_isSuspended)
                lock (_suspendingLock)
                    if (_isSuspended)
                    {
                        _waitingEvents.Enqueue(eventInfo);
                        return;
                    }

            SubscribeToEvents(new[] {eventInfo});
        }

        private void SubscribeToEvents(IEnumerable<EventInfo> events)
        {
            var eventsToSubscribe = new List<ulong>();

            lock (_eventSubscribingLock)
            {
                foreach (var eventInfo in events)
                {
                    if (_subscribedEvents.TryGetValue(eventInfo, out var counter))
                        _subscribedEvents[eventInfo] = counter + 1;
                    else
                    {
                        _subscribedEvents.Add(eventInfo, 1);
                        eventsToSubscribe.Add(eventInfo.GetEventId(_eventInterface, _eventSessionId));
                    }
                }
            }

            if (eventsToSubscribe.Count > 0)
                _eventManager.SubscribeEvents(this, eventsToSubscribe);
        }

        public void EventUnsubscribed(EventInfo eventInfo)
        {

        }

        public void SuspendSubscribing()
        {
            _isSuspended = true;
        }

        public void ResumeSubscribing()
        {
            List<EventInfo> eventsToSubscribe;
            lock (_suspendingLock)
            {
                _isSuspended = false;
                eventsToSubscribe = _waitingEvents.Distinct().ToList();
                _waitingEvents.Clear();
            }

            SubscribeToEvents(eventsToSubscribe);
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