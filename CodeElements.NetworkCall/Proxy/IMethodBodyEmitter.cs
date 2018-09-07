using System.Reflection;
using System.Reflection.Emit;

namespace CodeElements.NetworkCall.Proxy
{
	internal interface IMethodBodyEmitter
	{
		void EmitMethodBody(ILGenerator il, MethodInfo method, int methodIndex,
			FieldInfo field);
	}
}