using System.Reflection;

namespace CodeElements.NetworkCallTransmission.Proxy
{
    public interface IEventInterceptor
    {
        void EventSubscribed(EventInfo eventInfo);
        void EventUnsubscribed(EventInfo eventInfo);
    }
}