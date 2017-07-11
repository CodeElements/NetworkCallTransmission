using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeElements.NetworkCallTransmissionProtocol.Proxy
{
	internal class ProxyMethodBuilder
	{
		public ProxyMethodBuilder()
		{
			MethodBodyEmitter = new DefaultMethodEmitter();
		}

		public IMethodBodyEmitter MethodBodyEmitter { get; }

		public void CreateMethod(FieldInfo field, MethodInfo method, int methodIndex, TypeBuilder typeBuilder)
		{
			var parameters = method.GetParameters();
			var parameterTypes = parameters.Select(x => x.ParameterType).ToArray();

			MethodAttributes methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig |
			                                    MethodAttributes.Virtual;
			MethodBuilder methodBuilder = typeBuilder.DefineMethod(method.Name, methodAttributes,
				CallingConventions.HasThis, method.ReturnType,
				parameterTypes);

			var generator = methodBuilder.GetILGenerator();
			MethodBodyEmitter.EmitMethodBody(generator, method, methodIndex, field);
		}
	}
}