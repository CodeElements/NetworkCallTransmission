using System.Reflection;

namespace CodeElements.NetworkCallTransmission.Proxy
{
    public interface IEventInterceptorProxy
    {
        IEventInterceptor Interceptor { get; set; }
        EventInfo[] Events { get; set; }

        void TriggerEvent(int eventId, object transmissionInfo, object parameter);
    }
}