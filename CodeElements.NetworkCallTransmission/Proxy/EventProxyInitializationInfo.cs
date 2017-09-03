using System.Reflection;

namespace CodeElements.NetworkCallTransmission.Proxy
{
    internal class EventProxyInitializationInfo
    {
        public EventProxyInitializationInfo(TypeInfo proxyType, EventInfo[] events)
        {
            ProxyType = proxyType;
            Events = events;
        }

        public TypeInfo ProxyType { get; }
        public EventInfo[] Events { get; }
    }
}