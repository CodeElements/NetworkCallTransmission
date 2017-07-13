using System;
using System.Reflection;

namespace CodeElements.NetworkCallTransmission.Proxy
{
    internal class EventProxyInitializationInfo
    {
        public EventProxyInitializationInfo(Type proxyType, EventInfo[] events)
        {
            ProxyType = proxyType;
            Events = events;
        }

        public Type ProxyType { get; }
        public EventInfo[] Events { get; }
    }
}