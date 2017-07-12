using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CodeElements.NetworkCallTransmissionProtocol.Extensions;
using CodeElements.NetworkCallTransmissionProtocol.Internal;
using ZeroFormatter;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    internal class EventProxyEventArgs : EventArgs
    {
        public EventProxyEventArgs(ulong eventId, object parameter)
        {
            EventId = eventId;
            Parameter = parameter;
        }

        public ulong EventId { get; }
        public object Parameter { get; }
    }

    internal class EventSubscription
    {
        private readonly MethodInfo _addMethod;
        private readonly Delegate _dynamicHandler;
        private readonly object _obj;
        private readonly MethodInfo _removeMethod;
        private readonly object _subscribeLock = new object();

        public EventSubscription(EventInfo eventInfo, ulong eventId, Action<object[]> handler, object obj)
        {
            _obj = obj;
            EventId = eventId;
            _dynamicHandler = BuildDynamicHandler(eventInfo.EventHandlerType, handler, eventId);

            _addMethod = eventInfo.GetAddMethod();
            _removeMethod = eventInfo.GetRemoveMethod();

            Subscriber = new List<IEventSubscriber>();
            SubscriberLock = new object();
        }

        public ulong EventId { get; }
        public bool IsSubscribed { get; private set; }
        public List<IEventSubscriber> Subscriber { get; }
        public object SubscriberLock { get; }

        public void Subscribe()
        {
            if (IsSubscribed)
                return;

            lock (_subscribeLock)
            {
                if (IsSubscribed)
                    return;

                _addMethod.Invoke(_obj, new object[] {_dynamicHandler});
                IsSubscribed = true;
            }
        }

        public void Unsubscribe()
        {
            if (!IsSubscribed)
                return;

            lock (_subscribeLock)
            {
                if (!IsSubscribed)
                    return;

                _removeMethod.Invoke(_obj, new object[] {_dynamicHandler});
                IsSubscribed = false;
            }
        }

        private static Delegate BuildDynamicHandler(Type delegateType, Action<object[]> func, ulong id)
        {
            var invokeMethod = delegateType.GetMethod(nameof(EventHandler.Invoke));
            var parms = invokeMethod.GetParameters().Select(parm => Expression.Parameter(parm.ParameterType, parm.Name))
                .ToArray();
            var instance = func.Target == null ? null : Expression.Constant(func.Target);
            var converted = parms.Select(parm => (Expression) Expression.Convert(parm, typeof(object))).ToList();
            converted.Insert(0, Expression.Convert(Expression.Constant(id), typeof(object)));

            var call = Expression.Call(instance, func.Method, Expression.NewArrayInit(typeof(object), converted));
            var expr = Expression.Lambda(delegateType, call, parms);
            return expr.Compile();
        }
    }

    internal class EventSubscriber
    {
        public EventSubscriber(object eventProvider, Type type, uint sessionId)
        {
            var events = type.GetEvents();
            AvailableEvents = new EventSubscription[events.Length];
            for (var i = 0; i < events.Length; i++)
            {
                var eventInfo = events[i];
                AvailableEvents[i] = new EventSubscription(eventInfo, eventInfo.GetEventId(type, sessionId),
                    EventHandler, eventProvider);
            }

            Id = sessionId;
        }

        public uint Id { get; }
        public EventSubscription[] AvailableEvents { get; }

        public event EventHandler<EventProxyEventArgs> EventRaised;

        private void EventHandler(object[] objects)
        {
            var eventId = (ulong) objects[0];

            object parameter;
            //objects[1] is the instance
            if (objects.Length > 2)
                parameter = objects[2] == EventArgs.Empty ? null : objects[2];
            else
                parameter = null;

            EventRaised?.Invoke(this, new EventProxyEventArgs(eventId, parameter));
        }
    }

    public class EventRegister
    {
        private const int EstimatedParameterSize = 200;
        private readonly ConcurrentDictionary<IEventSubscriber, List<ulong>> _clients;
        private readonly ConcurrentDictionary<ulong, EventSubscription> _events;
        private int _eventIdCounter;

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
        /// <typeparam name="TEventInterface"></typeparam>
        /// <param name="eventInterface"></param>
        public void RegisterEvents<TEventInterface>(TEventInterface eventInterface)
        {
            var eventSubscriber = new EventSubscriber(eventInterface, typeof(TEventInterface), 0);
            AddEventSubscriber(eventSubscriber);
        }

        public uint RegisterPrivateEvents<TEventInterface>(TEventInterface eventInterface)
        {
            var id = (uint) Interlocked.Increment(ref _eventIdCounter);

            var eventSubscriber = new EventSubscriber(eventInterface, typeof(TEventInterface), id);
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

                //await because the byte array may be modified by the clients (CustomOffset)
                foreach (var eventClient in subscriber)
                    await eventClient.TriggerEvent(data, length).ConfigureAwait(false); //forget
            }
        }
    }

    public interface IEventSubscriber
    {
        Task TriggerEvent(byte[] data, int length);
    }
}