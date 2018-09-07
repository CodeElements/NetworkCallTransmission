using System.Reflection;

namespace CodeElements.NetworkCall.Proxy
{
	public interface IAsyncInterceptorProxy
	{
		IAsyncInterceptor Interceptor { get; set; }
		MethodInfo[] Methods { get; set; }
	}
}