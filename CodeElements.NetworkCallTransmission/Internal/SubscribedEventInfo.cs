using System;
using System.Collections.Generic;
using System.Reflection;

namespace CodeElements.NetworkCallTransmission.Internal
{
    internal class SubscribedEventInfo
    {
        public SubscribedEventInfo(EventInfo eventInfo)
        {
            EventInfo = eventInfo;
            Triggers = new List<IEventTrigger>();
            TriggersLock = new object();

            if (eventInfo.EventHandlerType.IsGenericType)
                EventHandlerParameterType = eventInfo.EventHandlerType.GetGenericArguments()[0];
        }

        public List<IEventTrigger> Triggers { get; }
        public EventInfo EventInfo { get; }
        public Type EventHandlerParameterType { get; }
        public object TriggersLock { get; }
    }
}