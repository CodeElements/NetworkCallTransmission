using System.Reflection;

namespace CodeElements.NetworkCall.Proxy
{
    public interface IEventInterceptorProxy
    {
        IEventInterceptor Interceptor { get; set; }
        EventInfo[] Events { get; set; }

        void TriggerEvent(int eventId, object parameter);
    }
}