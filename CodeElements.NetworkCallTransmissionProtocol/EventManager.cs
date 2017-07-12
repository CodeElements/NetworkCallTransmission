using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using CodeElements.NetworkCallTransmissionProtocol.Internal;
using CodeElements.NetworkCallTransmissionProtocol.Proxy;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    public class EventManager : DataTransmitter
    {
        private readonly ConcurrentDictionary<ulong, List<IEventTrigger>> _methodCalls;
        private readonly object _methodCallsAddLock = new object();

        public EventManager()
        {
            _methodCalls = new ConcurrentDictionary<ulong, List<IEventTrigger>>();
        }

        public IEventProvider<TEventInterface> GetEvents<TEventInterface>()
        {
            return GetEvents<TEventInterface>(0);
        }

        public IEventProvider<TEventInterface> GetEvents<TEventInterface>(uint eventSessionId)
        {
            var provider = new EventProvider<TEventInterface>(eventSessionId, typeof(TEventInterface), this);
            provider.Events = ProxyFactory.CreateProxy<TEventInterface>(provider);
            return provider;
        }

        internal void SubscribeEvents(IEventTrigger eventTrigger, IEnumerable<ulong> events)
        {
            var eventsToRegister = new List<ulong>();

            lock (_methodCallsAddLock)
            {
                foreach (var eventKey in events)
                {
                    if (!_methodCalls.TryGetValue(eventKey, out var eventInterceptors))
                    {
                        Debug.Assert(_methodCalls.TryAdd(eventKey,
                            eventInterceptors = new List<IEventTrigger>())); //Assert
                        eventsToRegister.Add(eventKey);
                    }

                    eventInterceptors.Add(eventTrigger);
                }
            }

            if (eventsToRegister.Count > 0)
                SendEventAction(true, eventsToRegister);
        }

        internal void UnsubscribeEvents(IEventTrigger eventTrigger, IEnumerable<ulong> events)
        {
            var eventsToUnregister = new List<ulong>();

            lock (_methodCallsAddLock)
            {
                foreach (var eventKey in events)
                {
                    if (_methodCalls.TryGetValue(eventKey, out var eventInterceptors))
                    {
                        eventInterceptors.Remove(eventTrigger);
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

            Buffer.BlockCopy(BitConverter.GetBytes((ushort) events.Count), 0, package, CustomOffset + 1, 2);
            for (var index = 0; index < events.Count; index++)
            {
                var eventId = events[index];
                Buffer.BlockCopy(BitConverter.GetBytes(eventId), 0, package, CustomOffset + 3 + index * 8, 8);
            }

            OnSendData(new ResponseData(package));
        }
    }
}