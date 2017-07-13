using System.Reflection;
using System.Threading.Tasks;

namespace CodeElements.NetworkCallTransmission.Proxy
{
	public interface IInvocation
	{
		MethodInfo MethodInfo { get; }
		object[] Arguments { get; }
		Task ReturnValue { get; set; }
	}
}