using System.Reflection;

namespace CodeElements.NetworkCallTransmissionProtocol.Proxy
{
    public interface IEventInterceptorProxy
    {
        IEventInterceptor Interceptor { get; set; }
        EventInfo[] Events { get; set; }
    }
}