using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodeElements.NetworkCall.Extensions;
using CodeElements.NetworkCall.Proxy;

namespace CodeElements.NetworkCall.Internal
{
    internal class NetworkCallEventProvider<TInterface> : IEventInterceptor, IEventTrigger
    {
        private readonly NetworkCallClient<TInterface> _networkCallClient;
        private bool _isSuspended;
        private readonly object _suspensionLock = new object();
        private readonly Queue<EventInfo> _waitingEvents;
        private readonly object _eventSubscribingLock = new object();
        private readonly Dictionary<EventInfo, int> _subscribedEvents;

        public NetworkCallEventProvider(NetworkCallClient<TInterface> networkCallClient)
        {
            _networkCallClient = networkCallClient;
            _waitingEvents = new Queue<EventInfo>();
            _subscribedEvents = new Dictionary<EventInfo, int>();
        }

        public IEventInterceptorProxy Proxy { get; set; }

        public void SuspendSubscribing()
        {
            _isSuspended = true;
        }

        public void ResumeSubscribing()
        {
            List<EventInfo> eventsToSubscribe;
            lock (_suspensionLock)
            {
                _isSuspended = false;
                eventsToSubscribe = _waitingEvents.Distinct().ToList();
                _waitingEvents.Clear();
            }

            SubscribeToEvents(eventsToSubscribe);
        }

        public void EventSubscribed(EventInfo eventInfo)
        {
            //it is no problem if an event is instantly subscribed even if it should be suspended but it is a problem
            //if an event is delayed subscribed if it was disabled (because ResumeSubscribing wont be called anymore)
            if (_isSuspended)
                lock (_suspensionLock)
                {
                    if (_isSuspended)
                    {
                        _waitingEvents.Enqueue(eventInfo);
                        return;
                    }
                }

            SubscribeToEvents(eventInfo.Yield());
        }

        private void SubscribeToEvents(IEnumerable<EventInfo> events)
        {
            var eventsToSubscribe = new List<(EventInfo, uint)>();

            lock (_eventSubscribingLock)
            {
                foreach (var eventInfo in events)
                {
                    if (_subscribedEvents.TryGetValue(eventInfo, out var counter))
                        _subscribedEvents[eventInfo] = counter + 1;
                    else
                    {
                        _subscribedEvents.Add(eventInfo, 1);
                        eventsToSubscribe.Add((eventInfo, eventInfo.GetEventId()));
                    }
                }
            }

            if (eventsToSubscribe.Count > 0)
                _networkCallClient.SubscribeEvents(eventsToSubscribe);
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
                _networkCallClient.UnsubscribeEvents((eventInfo, eventInfo.GetEventId()).Yield().ToList());
        }

        public void TriggerEvent(EventInfo eventInfo, object parameter)
        {
            var eventIndex = Array.IndexOf(Proxy.Events, eventInfo);
            Proxy.TriggerEvent(eventIndex, parameter);
        }
    }
}