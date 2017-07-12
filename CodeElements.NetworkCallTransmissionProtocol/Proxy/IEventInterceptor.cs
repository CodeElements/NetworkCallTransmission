using System.Reflection;

namespace CodeElements.NetworkCallTransmissionProtocol.Proxy
{
    public delegate void 

    public interface IEventInterceptor
    {
        void EventSubscribed(EventInfo eventInfo);
        void EventUnsubscribed(EventInfo eventInfo);
    }
}