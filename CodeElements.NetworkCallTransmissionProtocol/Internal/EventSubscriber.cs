using System;
using CodeElements.NetworkCallTransmissionProtocol.Extensions;

namespace CodeElements.NetworkCallTransmissionProtocol.Internal
{
    internal class EventSubscriber
    {
        public EventSubscriber(object eventProvider, Type type, uint sessionId)
        {
            var events = type.GetEvents();
            AvailableEvents = new EventSubscription[events.Length];
            for (var i = 0; i < events.Length; i++)
            {
                var eventInfo = events[i];

                var eventHandlerType = eventInfo.EventHandlerType;
                if(!(eventHandlerType == typeof(EventHandler) || eventHandlerType.IsGenericType && eventHandlerType.GetGenericTypeDefinition() == typeof(EventHandler<>)))
                    throw new ArgumentException("All events must be of type EventHandler or EventHandler<>", nameof(type));

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

            object parameter;
            //objects[1] is the instance
            if (objects.Length > 2)
                parameter = objects[2] == EventArgs.Empty ? null : objects[2];
            else
                parameter = null;

            EventRaised?.Invoke(this, new EventProxyEventArgs(eventId, parameter));
        }
    }
}