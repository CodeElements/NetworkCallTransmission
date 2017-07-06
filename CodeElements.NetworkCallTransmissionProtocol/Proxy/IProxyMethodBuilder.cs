using System.Reflection;
using System.Reflection.Emit;

namespace CodeElements.NetworkCallTransmissionProtocol.Proxy
{
	internal interface IProxyMethodBuilder
	{
		void CreateMethod(FieldInfo field, MethodInfo method, int methodIndex, TypeBuilder typeBuilder);
	}
}