using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

// ReSharper disable PossibleNullReferenceException

namespace CodeElements.NetworkCallTransmissionProtocol.Proxy
{
	internal class DefaultMethodEmitter : IMethodBodyEmitter
	{
		private readonly MethodInfo _asyncHandlerMethod;
		private readonly MethodInfo _asyncHandlerMethodGeneric;
		private readonly MethodInfo _getInterceptor;
		private readonly MethodInfo _getMethods;
		private readonly ConstructorInfo _invocationInfoCtor;
		private readonly MethodInfo _invocationReturn;

		public DefaultMethodEmitter()
		{
			_getMethods = typeof(IAsyncInterceptorProxy).GetProperty(nameof(IAsyncInterceptorProxy.Methods)).GetGetMethod();
			_getInterceptor = typeof(IAsyncInterceptorProxy).GetProperty(nameof(IAsyncInterceptorProxy.Interceptor)).GetGetMethod();
			_invocationReturn = typeof(InvocationInfo).GetProperty(nameof(InvocationInfo.ReturnValue)).GetGetMethod();

			var asyncInterceptorMethods = typeof(IAsyncInterceptor).GetMethods();
			_asyncHandlerMethod =
				asyncInterceptorMethods.First(x => x.Name == nameof(IAsyncInterceptor.InterceptAsynchronous) && !x.IsGenericMethod);
			_asyncHandlerMethodGeneric =
				asyncInterceptorMethods.First(x => x.Name == nameof(IAsyncInterceptor.InterceptAsynchronous) && x.IsGenericMethod);

			_invocationInfoCtor = typeof(InvocationInfo).GetConstructor(new[]
			{
				typeof(MethodInfo), typeof(object[])
			});
		}

		public void EmitMethodBody(ILGenerator il, MethodInfo method, int methodIndex, FieldInfo field)
		{
			Type actualReturnType;

			if (method.ReturnType == typeof(Task))
				actualReturnType = null;
			else if (method.ReturnType.IsGenericType &&
			         method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
				actualReturnType = method.ReturnType.GetGenericArguments()[0];
			else
				throw new ArgumentException("Only tasks are supported as return type.", method.ToString());


			var parameters = method.GetParameters();

			il.DeclareLocal(typeof(object[]));
			il.DeclareLocal(typeof(InvocationInfo));
			il.DeclareLocal(typeof(MethodInfo));

			//load all parameters in object[]
			PushArguments(parameters, il);

			//load the method
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Call, _getMethods);
			il.Emit(OpCodes.Ldc_I4, methodIndex);
			il.Emit(OpCodes.Ldelem_Ref); //load array[index]
			il.Emit(OpCodes.Stloc_2);

			//new InvocationInfo(method, object[])
			il.Emit(OpCodes.Ldloc_2);
			il.Emit(OpCodes.Ldloc_0);
			il.Emit(OpCodes.Newobj, _invocationInfoCtor);

			il.Emit(OpCodes.Stloc_1);

			il.Emit(OpCodes.Ldarg_0);


			il.Emit(OpCodes.Call, _getInterceptor);
			il.Emit(OpCodes.Ldloc_1);

			var interceptionMethod = actualReturnType == null
				? _asyncHandlerMethod
				: _asyncHandlerMethodGeneric.MakeGenericMethod(actualReturnType);

			il.Emit(OpCodes.Callvirt, interceptionMethod);

			il.Emit(OpCodes.Ldloc_1);
			il.Emit(OpCodes.Call, _invocationReturn);

			if (actualReturnType != null)
				il.Emit(OpCodes.Castclass, method.ReturnType);

			il.Emit(OpCodes.Ret);
		}

		private void PushArguments(ParameterInfo[] parameters, ILGenerator il)
		{
			var parameterCount = parameters?.Length ?? 0;

			// object[] args = new object[size];
			il.Emit(OpCodes.Ldc_I4, parameterCount);
			il.Emit(OpCodes.Newarr, typeof(object));
			il.Emit(OpCodes.Stloc_0);

			if (parameterCount == 0)
				return;

			// Populate the object array with the list of arguments
			var index = 0;
			var argumentPosition = 1;
			foreach (var param in parameters)
			{
				var parameterType = param.ParameterType;
				// args[N] = argumentN (pseudocode)
				il.Emit(OpCodes.Ldloc_S, 0);
				il.Emit(OpCodes.Ldc_I4, index);

				il.Emit(OpCodes.Ldarg, argumentPosition);
				if (parameterType.IsValueType)
					il.Emit(OpCodes.Box, parameterType);

				il.Emit(OpCodes.Stelem_Ref);

				index++;
				argumentPosition++;
			}
		}
	}
}
 