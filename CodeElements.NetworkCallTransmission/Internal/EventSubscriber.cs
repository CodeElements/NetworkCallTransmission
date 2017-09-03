using System;
using System.Reflection;
using CodeElements.NetworkCallTransmission.Extensions;

namespace CodeElements.NetworkCallTransmission.Internal
{
    internal class EventSubscriber
    {
        public EventSubscriber(object eventProvider, Type type, uint sessionId)
        {
            var events = type.GetTypeInfo().GetEvents();
            AvailableEvents = new EventSubscription[events.Length];
            for (var i = 0; i < events.Length; i++)
            {
                var eventInfo = events[i];

                var eventHandlerType = eventInfo.EventHandlerType;
                if (!(eventHandlerType.GetTypeInfo().IsGenericType &&
                      (eventHandlerType.GetGenericTypeDefinition() == typeof(TransmittedEventHandler<>) ||
                       eventHandlerType.GetGenericTypeDefinition() == typeof(TransmittedEventHandler<,>))))
                    throw new ArgumentException("All events must be of type TransmittedEventHandler<> or TransmittedEventHandler<,>",
                        nameof(type));

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
            var parameter = objects.Length > 2 ? objects[2] : null;

            EventRaised?.Invoke(this, new EventProxyEventArgs(eventId, objects[1], parameter));
        }
    }
}