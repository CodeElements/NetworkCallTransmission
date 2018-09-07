using System.Reflection;

namespace CodeElements.NetworkCall.Proxy
{
    public interface IEventInterceptor
    {
        void EventSubscribed(EventInfo eventInfo);
        void EventUnsubscribed(EventInfo eventInfo);
    }
}