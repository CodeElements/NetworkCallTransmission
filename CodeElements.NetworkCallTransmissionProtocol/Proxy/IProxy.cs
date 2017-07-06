using System.Reflection;

namespace CodeElements.NetworkCallTransmissionProtocol.Proxy
{
	public interface IProxy
	{
		IAsyncInterceptor Interceptor { get; set; }
		MethodInfo[] Methods { get; set; }
	}
}