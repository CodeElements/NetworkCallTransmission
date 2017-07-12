using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using CodeElements.NetworkCallTransmissionProtocol.Internal;
using CodeElements.NetworkCallTransmissionProtocol.Proxy;
using ZeroFormatter;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    internal class SubscribedEventInfo
    {
        public SubscribedEventInfo(EventInfo eventInfo)
        {
            EventInfo = eventInfo;
            Triggers = new List<IEventTrigger>();
            TriggersLock = new object();

            if (eventInfo.EventHandlerType.IsGenericTypeDefinition)
                EventHandlerParameterType = eventInfo.EventHandlerType.GetGenericArguments()[0];
        }

        public List<IEventTrigger> Triggers { get; }
        public EventInfo EventInfo { get; }
        public Type EventHandlerParameterType { get; }
        public object TriggersLock { get; }
    }

    public class EventManager : DataTransmitter
    {
        private readonly ConcurrentDictionary<ulong, SubscribedEventInfo> _subscribedEvents;
        private readonly object _methodCallsAddLock = new object();

        public EventManager()
        {
            _subscribedEvents = new ConcurrentDictionary<ulong, SubscribedEventInfo>();
        }

        /// <summary>
        /// Receive data from a <see cref="EventRegister"/>
        /// </summary>
        /// <param name="data">An array of bytes</param>
        /// <param name="offset">The starting position within the buffer</param>
        public override void ReceiveData(byte[] data, int offset)
        {
            var responseType = (EventResponseType) data[offset];
            switch (responseType)
            {
                case EventResponseType.TriggerEvent:
                case EventResponseType.TriggerEventWithParameter:
                    var withParameter = responseType == EventResponseType.TriggerEventWithParameter;
                    var eventId = BitConverter.ToUInt64(data, offset + 1);

                    List<IEventTrigger> triggers;
                    if (!_subscribedEvents.TryGetValue(eventId, out var subscribedEvent))
                        return;

                    lock (subscribedEvent.TriggersLock)
                    {
                        triggers = subscribedEvent.Triggers.ToList(); //copy list
                    }

                    if (withParameter)
                    {
                        var parameter =
                            ZeroFormatterSerializer.NonGeneric.Deserialize(subscribedEvent.EventHandlerParameterType,
                                data, 9);
                        foreach (var eventTrigger in triggers)
                        {
                            eventTrigger.GetType()
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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

        internal void SubscribeEvents(IEventTrigger eventTrigger, IEnumerable<Tuple<EventInfo, ulong>> events)
        {
            var eventsToRegister = new List<ulong>();

            foreach (var eventKey in events)
            {
                var subscribedInfo = new Lazy<SubscribedEventInfo>(() => new SubscribedEventInfo(eventKey.Item1), LazyThreadSafetyMode.None);
                var addedSubscribedInfo = _subscribedEvents.GetOrAdd(eventKey.Item2, l => subscribedInfo.Value);

                lock (addedSubscribedInfo.TriggersLock)
                    addedSubscribedInfo.Triggers.Add(eventTrigger);

                if (subscribedInfo.IsValueCreated)
                    eventsToRegister.Add(eventKey.Item2);
            }

            if (eventsToRegister.Count > 0)
                SendEventAction(true, eventsToRegister);
        }

        internal void UnsubscribeEvents(IEventTrigger eventTrigger, IEnumerable<ulong> events)
        {
            var eventsToUnregister = new List<ulong>();

            foreach (var eventKey in events)
            {
                if (_subscribedEvents.TryGetValue(eventKey, out var subscribedEventInfo))
                {
                    lock (subscribedEventInfo.TriggersLock)
                    {
                        subscribedEventInfo.Triggers.Remove(eventTrigger);
                        if (subscribedEventInfo.Triggers.Count == 0)
                        {
                            eventsToUnregister.Add(eventKey);
                            _subscribedEvents.TryRemove(eventKey, out var _);
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