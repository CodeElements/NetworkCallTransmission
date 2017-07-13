using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeElements.NetworkCallTransmission.Internal;
using ZeroFormatter;

namespace CodeElements.NetworkCallTransmission
{
    /// <summary>
    ///     Let classes register their events to make them available to the remote side. The counterpart is
    ///     <see cref="EventManager" />
    /// </summary>
    public class EventRegister
    {
        private const int EstimatedParameterSize = 200;
        private readonly ConcurrentDictionary<IEventSubscriber, List<ulong>> _clients;
        private readonly ConcurrentDictionary<ulong, EventSubscription> _events;
        private int _eventIdCounter;

        /// <summary>
        ///     Initialize a new instance of <see cref="EventRegister" />
        /// </summary>
        public EventRegister()
        {
            _events = new ConcurrentDictionary<ulong, EventSubscription>();
            _clients = new ConcurrentDictionary<IEventSubscriber, List<ulong>>();
            _eventIdCounter = 1;
        }

        /// <summary>
        ///     Reserve bytes at the beginning of the data buffer for custom headers
        /// </summary>
        public int CustomOffset { get; set; }

        /// <summary>
        ///     Register events
        /// </summary>
        /// <typeparam name="TEventInterface">
        ///     The event contract interface which defines the events. Please note that all events
        ///     must be an <see cref="EventHandler" /> or <see cref="EventHandler{TEventArgs}" />.
        /// </typeparam>
        /// <param name="eventProvider">The instance of <see cref="TEventInterface"/> which calls the events</param>
        public void RegisterEvents<TEventInterface>(TEventInterface eventProvider) where TEventInterface : class
        {
            var interfaceType = typeof(TEventInterface);
            if (!interfaceType.IsInterface)
                throw new ArgumentException("TEventInterface must be an interface", nameof(TEventInterface));

            var eventSubscriber = new EventSubscriber(eventProvider, typeof(TEventInterface), 0);
            AddEventSubscriber(eventSubscriber);
        }

        /// <summary>
        ///     Register private events with a session id
        /// </summary>
        /// <typeparam name="TEventInterface">
        ///     The event contract interface which defines the events. Please note that all events
        ///     must be an <see cref="EventHandler" /> or <see cref="EventHandler{TEventArgs}" />.
        /// </typeparam>
        /// <param name="eventProvider">The instance of <see cref="TEventInterface"/> which calls the events</param>
        /// <returns>Return the session id</returns>
        public uint RegisterPrivateEvents<TEventInterface>(TEventInterface eventProvider)
        {
            var id = (uint) Interlocked.Increment(ref _eventIdCounter);

            var eventSubscriber = new EventSubscriber(eventProvider, typeof(TEventInterface), id);
            AddEventSubscriber(eventSubscriber);

            return id;
        }

        /// <summary>
        ///     A response was received. Warning: This function is not thread safe for every single client; the same
        ///     <see cref="IEventSubscriber" /> must not call this method from different threads, but different
        ///     <see cref="IEventSubscriber" /> can call the method from different threads
        /// </summary>
        /// <param name="data">An array of bytes</param>
        /// <param name="offset">The starting position within the buffer</param>
        /// <param name="eventSubscriber">The subscribing client</param>
        public void ReceiveResponse(byte[] data, int offset, IEventSubscriber eventSubscriber)
        {
            var prefix = (EventPackageType) data[offset];

            switch (prefix)
            {
                case EventPackageType.SubscribeEvent:
                case EventPackageType.UnsubscribeEvent:
                    var subscribe = prefix == EventPackageType.SubscribeEvent;
                    var eventCount = BitConverter.ToUInt16(data, offset + 1);
                    var events = new List<ulong>(eventCount);

                    for (var i = 0; i < eventCount; i++)
                        events.Add(BitConverter.ToUInt64(data, 3 + i * 8));

                    foreach (var eventId in events)
                    {
                        if (!_events.TryGetValue(eventId, out var eventSubscription))
                            continue;

                        lock (eventSubscription.SubscriberLock)
                        {
                            if (subscribe)
                                eventSubscription.Subscriber.Add(eventSubscriber);
                            else
                                eventSubscription.Subscriber.Remove(eventSubscriber);

                            if (subscribe)
                                eventSubscription.Subscribe();
                            else if (eventSubscription.Subscriber.Count == 0)
                                eventSubscription.Unsubscribe();
                        }

                        if (!_clients.TryGetValue(eventSubscriber, out var registeredEvents))
                        {
                            if (subscribe)
                                _clients.TryAdd(eventSubscriber, new List<ulong> {eventId});
                        }
                        else
                        {
                            if (subscribe)
                                registeredEvents.Add(eventId);
                            else
                                registeredEvents.Remove(eventId);
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AddEventSubscriber(EventSubscriber eventSubscriber)
        {
            eventSubscriber.EventRaised += EventSubscriberOnEventRaised;

            foreach (var availableEvent in eventSubscriber.AvailableEvents)
                _events.TryAdd(availableEvent.EventId, availableEvent);
        }

        private async void EventSubscriberOnEventRaised(object sender, EventProxyEventArgs eventProxyEventArgs)
        {
            if (_events.TryGetValue(eventProxyEventArgs.EventId, out var subscription))
            {
                byte[] data;
                int length;

                if (eventProxyEventArgs.Parameter == null)
                {
                    data = new byte[CustomOffset + 9];
                    data[CustomOffset] = (byte) EventResponseType.TriggerEvent;
                    Buffer.BlockCopy(BitConverter.GetBytes(eventProxyEventArgs.EventId), 0, data, CustomOffset + 1, 8);
                    length = data.Length;
                }
                else
                {
                    data = new byte[EstimatedParameterSize + 9 + CustomOffset];
                    data[CustomOffset] = (byte) EventResponseType.TriggerEventWithParameter;
                    Buffer.BlockCopy(BitConverter.GetBytes(eventProxyEventArgs.EventId), 0, data, CustomOffset + 1, 8);
                    length = CustomOffset + 9 +
                             ZeroFormatterSerializer.NonGeneric.Serialize(eventProxyEventArgs.Parameter.GetType(),
                                 ref data, CustomOffset + 9, eventProxyEventArgs.Parameter);
                }

                List<IEventSubscriber> subscriber;
                lock (subscription.SubscriberLock)
                {
                    subscriber = subscription.Subscriber.ToList(); //copy subscriber
                }

                //check permissions
                IEnumerable<IEventSubscriber> validSubscriber;
                if (subscription.RequiredPermissions == null)
                    validSubscriber = subscriber;
                else
                {
                    var subscriberTasks = subscriber
                        .Select(x => new Tuple<Task<bool>, IEventSubscriber>(
                            x.CheckPermissions(subscription.RequiredPermissions, eventProxyEventArgs.Parameter), x))
                        .ToList();
                    var results = await Task.WhenAll(subscriberTasks.Select(x => x.Item1)).ConfigureAwait(false);

                    var validSubscriberList = new List<IEventSubscriber>();
                    for (int i = 0; i < subscriberTasks.Count; i++)
                    {
                        if (results[i])
                            validSubscriberList.Add(subscriberTasks[i].Item2);
                    }

                    validSubscriber = validSubscriberList;
                }

                //await because the byte array may be modified by the clients (CustomOffset)
                foreach (var eventClient in validSubscriber)
                    await eventClient.TriggerEvent(data, length).ConfigureAwait(false); //forget
            }
        }
    }
}