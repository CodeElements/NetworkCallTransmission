using System.Reflection;

namespace CodeElements.NetworkCallTransmission.Proxy
{
	public interface IAsyncInterceptorProxy
	{
		IAsyncInterceptor Interceptor { get; set; }
		MethodInfo[] Methods { get; set; }
	}
}