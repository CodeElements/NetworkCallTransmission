using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using CodeElements.NetworkCallTransmission.Internal;
using CodeElements.NetworkCallTransmission.Proxy;

namespace CodeElements.NetworkCallTransmission
{
    /// <summary>
    ///     Provides methods to subscribe to the remote events. The counterpart is <see cref="EventRegister" />
    /// </summary>
    public class EventManager : DataTransmitter, IEventManager
    {
        private readonly INetworkSerializer _serializer;
        private readonly ConcurrentDictionary<ulong, SubscribedEventInfo> _subscribedEvents;
        private readonly ConcurrentDictionary<Type, Lazy<EventProxyInitializationInfo>> _interfaceGeneratedTypes;

        /// <summary>
        ///     Initialize a new instance of <see cref="EventManager" />
        /// </summary>
        /// <param name="serializer">The serializer used to serialize/deserialize the objects</param>
        public EventManager(INetworkSerializer serializer)
        {
            _serializer = serializer;
            _subscribedEvents = new ConcurrentDictionary<ulong, SubscribedEventInfo>();
            _interfaceGeneratedTypes = new ConcurrentDictionary<Type, Lazy<EventProxyInitializationInfo>>();
        }

        /// <summary>
        ///     Get events from the remote side
        /// </summary>
        /// <typeparam name="TEventInterface">
        ///     The event contract interface which defines the events. Please note that all events
        ///     must be an <see cref="TransmittedEventHandler{TTransmissionInfo}" /> or <see cref="TransmittedEventHandler{TTransmissionInfo,TEventArgs}" />.
        /// </typeparam>
        /// <returns>Return an <see cref="IEventProvider{TEventInterface}" /> which manages the events</returns>
        public IEventProvider<TEventInterface> GetEvents<TEventInterface>()
        {
            return GetEvents<TEventInterface>(0);
        }

        /// <summary>
        ///     Get events from the remote side from a session
        /// </summary>
        /// <typeparam name="TEventInterface">
        ///     The event contract interface which defines the events. Please note that all events
        ///     must be an <see cref="TransmittedEventHandler{TTransmissionInfo}" /> or <see cref="TransmittedEventHandler{TTransmissionInfo,TEventArgs}" />.
        /// </typeparam>
        /// <param name="eventSessionId">The session id the events were registered with</param>
        /// <returns>Return an <see cref="IEventProvider{TEventInterface}" /> which manages the events</returns>
        public IEventProvider<TEventInterface> GetEvents<TEventInterface>(uint eventSessionId)
        {
            //we use a lazy here because ProxyFactory.CreateProxy is a really intensitive function (a new assembly is built and loaded)
            //and we only want it to execute once (thats the reason for the cache). The problem is that msdn says for GetOrAdd():
            //"If you call GetOrAdd simultaneously on different threads, addValueFactory may be called multiple times [...]"
            //see: https://stackoverflow.com/questions/12611167/why-does-concurrentdictionary-getoraddkey-valuefactory-allow-the-valuefactory

            var lazy = new Lazy<EventProxyInitializationInfo>(ProxyFactory.CreateProxy<TEventInterface>,
                LazyThreadSafetyMode.ExecutionAndPublication);

            var initializationInfo = _interfaceGeneratedTypes.GetOrAdd(typeof(TEventInterface), lazy);

            var provider = new EventProvider<TEventInterface>(eventSessionId, typeof(TEventInterface), this);
            provider.Events = ProxyFactory.InitializeEventProxy<TEventInterface>(initializationInfo.Value, provider);
            return provider;
        }

        /// <summary>
        ///     Receive data from a <see cref="EventRegister" />
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
                case EventResponseType.TriggerEventWithTransmissionInfo:
                case EventResponseType.TriggerEventWithTransmissionInfoAndParameter:
                    var eventId = BitConverter.ToUInt64(data, offset + 1);

                    List<IEventTrigger> triggers;
                    if (!_subscribedEvents.TryGetValue(eventId, out var subscribedEvent))
                        return;

                    lock (subscribedEvent.TriggersLock)
                    {
                        triggers = subscribedEvent.Triggers.ToList(); //copy list
                    }

                    object parameter;
                    object transmissionInfo;

                    if (responseType == EventResponseType.TriggerEvent)
                    {
                        transmissionInfo = null;
                        parameter = null;
                    }
                    else
                    {
                        var transmissionInfoLength = BitConverter.ToUInt16(data, offset + 9);
                        if (transmissionInfoLength > 0)
                            transmissionInfo = _serializer.Deserialize(subscribedEvent.EventHandlerTransmissionInfoType,
                                data, offset + 11);
                        else
                            transmissionInfo = null;

                        if (responseType == EventResponseType.TriggerEventWithParameter || responseType ==
                            EventResponseType.TriggerEventWithTransmissionInfoAndParameter)
                            parameter = _serializer.Deserialize(subscribedEvent.EventHandlerParameterType, data,
                                offset + 11 + transmissionInfoLength);
                        else
                            parameter = null;
                    }

                    foreach (var eventTrigger in triggers)
                        eventTrigger.TriggerEvent(subscribedEvent.EventInfo, transmissionInfo, parameter);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal void SubscribeEvents(IEventTrigger eventTrigger, IEnumerable<Tuple<EventInfo, ulong>> events)
        {
            var eventsToRegister = new List<ulong>();

            foreach (var eventKey in events)
            {
                var subscribedInfo = new Lazy<SubscribedEventInfo>(() => new SubscribedEventInfo(eventKey.Item1),
                    LazyThreadSafetyMode.None);
                var addedSubscribedInfo = _subscribedEvents.GetOrAdd(eventKey.Item2, l => subscribedInfo.Value);

                lock (addedSubscribedInfo.TriggersLock)
                {
                    addedSubscribedInfo.Triggers.Add(eventTrigger);
                }

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
                if (_subscribedEvents.TryGetValue(eventKey, out var subscribedEventInfo))
                    lock (subscribedEventInfo.TriggersLock)
                    {
                        subscribedEventInfo.Triggers.Remove(eventTrigger);
                        if (subscribedEventInfo.Triggers.Count == 0)
                        {
                            eventsToUnregister.Add(eventKey);
                            _subscribedEvents.TryRemove(eventKey, out var _);
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

            OnSendData(new ArraySegment<byte>(package));
        }
    }
}