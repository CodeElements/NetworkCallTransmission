using System.Reflection;

namespace CodeElements.NetworkCallTransmissionProtocol.Proxy
{
	public interface IAsyncInterceptorProxy
	{
		IAsyncInterceptor Interceptor { get; set; }
		MethodInfo[] Methods { get; set; }
	}
}