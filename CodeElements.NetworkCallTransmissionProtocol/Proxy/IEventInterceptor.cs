using System.Reflection;

namespace CodeElements.NetworkCallTransmissionProtocol.Proxy
{
    public interface IEventInterceptor
    {
        void EventSubscribed(EventInfo eventInfo);
        void EventUnsubscribed(EventInfo eventInfo);
        void Dispose();
    }
}