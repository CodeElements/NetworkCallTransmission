using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodeElements.NetworkCallTransmission.Extensions;
using CodeElements.NetworkCallTransmission.Proxy;

namespace CodeElements.NetworkCallTransmission.Internal
{
    internal class EventProvider<TEvents> : IEventProvider<TEvents>, IEventInterceptor, IEventTrigger
    {
        private readonly uint _eventSessionId;
        private readonly Type _eventInterface;
        private readonly EventManager _eventManager;
        private bool _isSuspended;
        private readonly Queue<EventInfo> _waitingEvents;
        private readonly Dictionary<EventInfo, int> _subscribedEvents;
        private readonly object _eventSubscribingLock = new object();
        private readonly object _suspendingLock = new object();
        private IEventInterceptorProxy _interceptorProxy;
        private TEvents _events;
        private readonly List<IEventFilter> _filters;

        public EventProvider(uint eventSessionId, Type eventInterface, EventManager eventManager)
        {
            _eventSessionId = eventSessionId;
            _eventInterface = eventInterface;
            _eventManager = eventManager;
            _waitingEvents = new Queue<EventInfo>();
            _subscribedEvents = new Dictionary<EventInfo, int>();
            _filters = new List<IEventFilter>();
        }

        public void Dispose()
        {
            IEnumerable<ulong> events;
            lock (_eventSubscribingLock)
            {
                //ToList() is very important!!!
                events = _subscribedEvents.Select(x => x.Key.GetEventId(_eventInterface, _eventSessionId)).ToList();
                _subscribedEvents.Clear();
            }

            _eventManager.UnsubscribeEvents(this, events);
        }

        public TEvents Events
        {
            get => _events;
            set
            {
                _events = value;
                _interceptorProxy = value as IEventInterceptorProxy;
            }
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
            var eventsToSubscribe = new List<Tuple<EventInfo, ulong>>();

            lock (_eventSubscribingLock)
            {
                foreach (var eventInfo in events)
                {
                    if (_subscribedEvents.TryGetValue(eventInfo, out var counter))
                        _subscribedEvents[eventInfo] = counter + 1;
                    else
                    {
                        _subscribedEvents.Add(eventInfo, 1);
                        eventsToSubscribe.Add(
                            new Tuple<EventInfo, ulong>(eventInfo, eventInfo.GetEventId(_eventInterface, _eventSessionId)));
                    }
                }
            }

            if (eventsToSubscribe.Count > 0)
                _eventManager.SubscribeEvents(this, eventsToSubscribe);
        }

        public void EventUnsubscribed(EventInfo eventInfo)
        {
            var unsubscribe = false;

            lock (_eventSubscribingLock)
            {
                if (!_subscribedEvents.TryGetValue(eventInfo, out var counter))
                    return;

                counter -= 1;
                if (counter == 0)
                {
                    unsubscribe = true;
                    _subscribedEvents.Remove(eventInfo);
                }
                else
                    _subscribedEvents[eventInfo] = counter;
            }

            if (unsubscribe)
                _eventManager.UnsubscribeEvents(this, new[] {eventInfo.GetEventId(_eventInterface, _eventSessionId)});
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

        public void AddFilter(IEventFilter eventFilter)
        {
            _filters.Add(eventFilter);
        }

        public void TriggerEvent(EventInfo eventInfo, object parameter)
        {
            if (_filters.Any(x => !x.FilterEvent(eventInfo, parameter)))
                return;

            var eventIndex = Array.IndexOf(_interceptorProxy.Events, eventInfo);
            _interceptorProxy.TriggerEvent(eventIndex, parameter);
        }
    }
}