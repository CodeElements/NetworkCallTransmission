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
    public class EventProxyEventArgs : EventArgs
    {
        public EventProxyEventArgs(ulong eventId, object parameter)
        {
            EventId = eventId;
            Parameter = parameter;
        }

        public ulong EventId { get; }
        public object Parameter { get; }
    }

    public class EventSubscription
    {
        private readonly object _obj;
        private readonly Delegate _dynamicHandler;
        private readonly MethodInfo _addMethod;
        private readonly MethodInfo _removeMethod;
        private readonly object _subscribeLock = new object();

        public EventSubscription(EventInfo eventInfo, ulong eventId, Action<object[]> handler, object obj)
        {
            _obj = obj;
            EventId = eventId;
            _dynamicHandler = BuildDynamicHandler(eventInfo.EventHandlerType, handler, eventId);

            _addMethod = eventInfo.GetAddMethod();
            _removeMethod = eventInfo.GetRemoveMethod();

            Subscriber = new List<IEventSubcriber>();
            SubscriberLock = new object();
        }

        public ulong EventId { get; }
        public bool IsSubscribed { get; private set; }
        public List<IEventSubcriber> Subscriber { get; }
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
            var parms = invokeMethod.GetParameters().Select(parm => Expression.Parameter(parm.ParameterType, parm.Name)).ToArray();
            var instance = func.Target == null ? null : Expression.Constant(func.Target);
            var converted = parms.Select(parm => (Expression)Expression.Convert(parm, typeof(object))).ToList();
            converted.Insert(0, Expression.Convert(Expression.Constant(id), typeof(object)));

            var call = Expression.Call(instance, func.Method, Expression.NewArrayInit(typeof(object), converted));
            var expr = Expression.Lambda(delegateType, call, parms);
            return expr.Compile();
        }
    }

    public class EventSubscriber
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

        public event EventHandler<EventProxyEventArgs> EventRaised;

        public uint Id { get; }
        public EventSubscription[] AvailableEvents { get; }

        private void EventHandler(object[] objects)
        {
            var eventId = (ulong) objects[0];
            var parameter = objects.Length > 2 ? objects[2] : null; //objects[1] is the instance
            EventRaised?.Invoke(this, new EventProxyEventArgs(eventId, parameter));
        }
    }

    public class EventRegister : DataTransmitter
    {
        private readonly ConcurrentDictionary<ulong, EventSubscription> _events;
        private readonly ConcurrentDictionary<IEventSubcriber, List<ulong>> _clients;
        private int _eventIdCounter;
        private const int EstimatedParameterSize = 200;

        public EventRegister()
        {
            _events = new ConcurrentDictionary<ulong, EventSubscription>();
            _clients = new ConcurrentDictionary<IEventSubcriber, List<ulong>>();
            _eventIdCounter = 1;
        }

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

                List<IEventSubcriber> subscriber;
                lock (subscription.SubscriberLock)
                    subscriber = subscription.Subscriber.ToList(); //copy subscriber

                //await because the byte array may be modified by the clients (CustomOffset)
                foreach (var eventClient in subscriber)
                    await eventClient.TriggerEvent(data, length).ConfigureAwait(false); //forget
            }
        }

        /// <summary>
        /// A response was received. Warning: This function is thread safe for all clients, meaning that it is not thread safe for a single client
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="eventSubscriber"></param>
        public void ReceiveResponse(byte[] data, int offset, IEventSubcriber eventSubscriber)
        {
            var prefix = (EventPackageType) data[offset];

            switch (prefix)
            {
                case EventPackageType.SubscribeEvent:
                case EventPackageType.UnsubscribeEvent:
                    var subscribe = prefix == EventPackageType.SubscribeEvent;
                    var eventCount = BitConverter.ToUInt16(data, offset + 1);
                    var events = new List<ulong>(eventCount);

                    for (int i = 0; i < eventCount; i++)
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
                case EventPackageType.Clear:

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }
    }

    public interface IEventSubcriber
    {
        Task TriggerEvent(byte[] data, int length);
    }
}