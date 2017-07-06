using System.Reflection;
using System.Threading.Tasks;

namespace CodeElements.NetworkCallTransmissionProtocol.Proxy
{
	public class InvocationInfo : IInvocation
	{
		public InvocationInfo(MethodInfo methodInfo, object[] arguments)
		{
			MethodInfo = methodInfo;
			Arguments = arguments;
		    ReturnValue = Task.FromResult((object) null);
		}

		public MethodInfo MethodInfo { get; }
		public object[] Arguments { get; }
		public Task ReturnValue { get; set; }
	}
}