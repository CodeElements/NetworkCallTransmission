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

            var genericArguments = eventInfo.EventHandlerType.GetGenericArguments();
            EventHandlerTransmissionInfoType = genericArguments[0];

            if (genericArguments.Length == 2)
                EventHandlerParameterType = genericArguments[1];
        }

        public List<IEventTrigger> Triggers { get; }
        public EventInfo EventInfo { get; }
        public Type EventHandlerTransmissionInfoType { get; }
        public Type EventHandlerParameterType { get; }
        public object TriggersLock { get; }
    }
}