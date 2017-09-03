using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CodeElements.NetworkCallTransmission.Internal;
using CodeElements.NetworkCallTransmission.Memory;

namespace CodeElements.NetworkCallTransmission
{
    /// <summary>
    ///     A delegate to modify the data before it is sent
    /// </summary>
    /// <param name="buffer">The data buffer</param>
    /// <param name="offset">The offset of the data in the buffer</param>
    /// <param name="length">The length of the data in the buffer</param>
    /// <returns>Return the modified byte array. If changes were made to buffer, just return it.
    /// </returns>
    public delegate Task<byte[]> ModifyDataDelegate(byte[] buffer, int offset, int length);

    /// <summary>
    ///     Let classes register their events to make them available to the remote side. The counterpart is
    ///     <see cref="EventManager" />
    /// </summary>
    public class EventRegister
    {
        internal const int MaxBufferSize = 2000;
        internal const int DefaultPoolSize = MaxBufferSize * 524; // ~1 MiB

        private const int EstimatedParameterSize = 150;
        private const int EstimatedTransmissionInfoSize = 16;
        private readonly BufferManager _bufferManager;
        private readonly ConcurrentDictionary<IEventSubscriber, List<ulong>> _clients;
        private readonly ConcurrentDictionary<ulong, EventSubscription> _events;
        private readonly INetworkSerializer _serializer;
        private int _eventIdCounter;

        /// <summary>
        ///     Initialize a new instance of <see cref="EventRegister" />
        /// </summary>
        /// <param name="serializer">The serializer used to serialize/deserialize the objects</param>
        public EventRegister(INetworkSerializer serializer) : this(serializer, DefaultPoolSize)
        {
        }

        /// <summary>
        ///     Initialize a new instance of <see cref="EventRegister" />
        /// </summary>
        /// <param name="serializer">The serializer used to serialize/deserialize the objects</param>
        /// <param name="totalBufferCacheSize">
        ///     The size of the thread-shared buffer for object serialization. Submit zero if you
        ///     dont want a global buffer
        /// </param>
        public EventRegister(INetworkSerializer serializer, long totalBufferCacheSize)
        {
            _serializer = serializer;
            _events = new ConcurrentDictionary<ulong, EventSubscription>();
            _clients = new ConcurrentDictionary<IEventSubscriber, List<ulong>>();
            _eventIdCounter = 1;
            _bufferManager = BufferManager.CreateBufferManager(totalBufferCacheSize, MaxBufferSize);
        }

        /// <summary>
        ///     Reserve bytes at the beginning of the data buffer for custom headers
        /// </summary>
        public int CustomOffset { get; set; }

        /// <summary>
        ///     Delegate that gets executed everytime before an event is triggered. It allows the modification of the data, like
        ///     setting a header (in the range of <see cref="CustomOffset" />) or compressing
        /// </summary>
        public ModifyDataDelegate ModifyDataDelegate { get; set; }

        /// <summary>
        ///     Register events
        /// </summary>
        /// <typeparam name="TEventInterface">
        ///     The event contract interface which defines the events. Please note that all events
        ///     must be an <see cref="EventHandler" /> or <see cref="EventHandler{TEventArgs}" />.
        /// </typeparam>
        /// <param name="eventProvider">The instance of <see cref="TEventInterface" /> which calls the events</param>
        public void RegisterEvents<TEventInterface>(TEventInterface eventProvider) where TEventInterface : class
        {
            var interfaceType = typeof(TEventInterface).GetTypeInfo();
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
        /// <param name="eventProvider">The instance of <see cref="TEventInterface" /> which calls the events</param>
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
                        events.Add(BitConverter.ToUInt64(data, offset + 3 + i * 8));

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
                byte[] data = null;
                var length = 0;
                var returnBuffer = true;

                async Task GetData()
                {
                    var dataLength = CustomOffset + 11 +
                                     (eventProxyEventArgs.TransmissionInfo != null
                                         ? EstimatedTransmissionInfoSize
                                         : 0) +
                                     (eventProxyEventArgs.EventArgs != null ? EstimatedParameterSize : 0);
                    var takenBuffer = _bufferManager.TakeBuffer(dataLength);
                    data = takenBuffer;

                    var transmissionInfoLength = 0;
                    var parameterLength = 0;

                    if (eventProxyEventArgs.TransmissionInfo != null)
                        transmissionInfoLength = _serializer.Serialize(subscription.TransmissionInfoType,
                            ref data, CustomOffset + 11, eventProxyEventArgs.TransmissionInfo);
                    if (eventProxyEventArgs.EventArgs != null)
                        parameterLength =
                            _serializer.Serialize(subscription.EventArgsType,
                                ref data, CustomOffset + 11 + transmissionInfoLength, eventProxyEventArgs.EventArgs);

                    EventResponseType responseType;
                    if (transmissionInfoLength > 0 && parameterLength > 0)
                        responseType = EventResponseType.TriggerEventWithTransmissionInfoAndParameter;
                    else if (transmissionInfoLength == 0 && parameterLength > 0)
                        responseType = EventResponseType.TriggerEventWithParameter;
                    else if (transmissionInfoLength > 0 && parameterLength == 0)
                        responseType = EventResponseType.TriggerEventWithTransmissionInfo;
                    else
                        responseType = EventResponseType.TriggerEvent;

                    data[CustomOffset] = (byte) responseType;
                    Buffer.BlockCopy(BitConverter.GetBytes(eventProxyEventArgs.EventId), 0, data, CustomOffset + 1, 8);

                    if (responseType == EventResponseType.TriggerEvent)
                    {
                        length = CustomOffset + 9;
                    }
                    else
                    {
                        Buffer.BlockCopy(BitConverter.GetBytes((ushort) transmissionInfoLength), 0, data,
                            CustomOffset + 9, 2);
                        length = CustomOffset + 11 + transmissionInfoLength + parameterLength;
                    }

                    if (ModifyDataDelegate != null)
                        data = await ModifyDataDelegate(data, 0, length).ConfigureAwait(false);

                    if (data != takenBuffer)
                    {
                        _bufferManager.ReturnBuffer(takenBuffer);
                        returnBuffer = false;
                    }
                }

                List<IEventSubscriber> subscribers;
                lock (subscription.SubscriberLock)
                {
                    subscribers = subscription.Subscriber.ToList(); //copy subscribers
                }

                if (subscribers.Count == 0)
                    return;

                List<Task> runningTasks;

                //check permissions
                if (subscription.RequiredPermissions == null)
                {
                    await GetData().ConfigureAwait(false);
                    runningTasks = subscribers.Select(x => x.TriggerEvent(data, 0, length)).ToList();
                }
                else
                {
                    var subscriberPermissionsTaskDictionary =
                        subscribers.ToDictionary(
                            x => x.CheckPermissions(subscription.RequiredPermissions,
                                eventProxyEventArgs.TransmissionInfo),
                            y => y);
                    var tasks = subscriberPermissionsTaskDictionary.Select(x => x.Key).ToList();
                    runningTasks = new List<Task>();

                    while (tasks.Count > 0)
                    {
                        var finishedTask = await Task.WhenAny(tasks).ConfigureAwait(false);
                        var subscriber = subscriberPermissionsTaskDictionary[finishedTask];
                        subscriberPermissionsTaskDictionary.Remove(finishedTask);
                        tasks.Remove(finishedTask);

                        if (finishedTask.Result)
                        {
                            if (data == null) await GetData().ConfigureAwait(false);
                            runningTasks.Add(subscriber.TriggerEvent(data, 0, length));
                        }
                    }
                }

                if (runningTasks.Count > 0)
                {
                    await Task.WhenAll(runningTasks).ConfigureAwait(false);
                    if (returnBuffer)
                        _bufferManager.ReturnBuffer(data);
                }
            }
        }
    }
}