using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeElements.NetworkCallTransmissionProtocol.Proxy
{
	internal static class ProxyFactory
	{
		private static readonly ConstructorInfo BaseConstructor = typeof(object).GetConstructor(new Type[0]);

		public static T CreateProxy<T>(IAsyncInterceptor asyncInterceptor)
		{
			var interfaceType = typeof(T);

			var currentDomain = AppDomain.CurrentDomain;
			var typeName = $"{typeof(T).Name}Proxy";
			var assemblyName = $"{typeName}Assembly";
			var moduleName = $"{typeName}Module";

			var name = new AssemblyName(assemblyName);

		    var access = AssemblyBuilderAccess.Run;
            var assemblyBuilder = currentDomain.DefineDynamicAssembly(name, access);
		    var moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName);
            var typeAttributes = TypeAttributes.AutoClass | TypeAttributes.Class |
			                                TypeAttributes.Public;

			var interfaceList = new List<Type> {interfaceType};
			BuildInterfaceList(interfaceType, interfaceList);

			var typeBuilder =
				moduleBuilder.DefineType(typeName, typeAttributes, typeof(object), interfaceList.ToArray());

			DefineConstructor(typeBuilder);

			//Implement IProxy
			var implementor = new InterceptorImplementor();
			implementor.ImplementProxy(typeBuilder);

			var methods = GetMethods(interfaceList);

			var builder = new ProxyMethodBuilder();
			for (var i = 0; i < methods.Count; i++)
			{
				builder.CreateMethod(implementor.InterceptorField, methods[i], i, typeBuilder);
			}

			var proxyType = typeBuilder.CreateType();
			var result = (T) Activator.CreateInstance(proxyType);

			var proxy = (IProxy) result;
			proxy.Interceptor = asyncInterceptor;
			proxy.Methods = methods.ToArray();

			return result;
		}

		private static void BuildInterfaceList(Type currentType, ICollection<Type> interfaceTypes)
		{
			var interfaces = currentType.GetInterfaces();
			if(interfaces.Length > 0)
				foreach (var type in interfaces)
				{
					if (interfaceTypes.Contains(type))
						continue;

					interfaceTypes.Add(type);
					BuildInterfaceList(type, interfaceTypes);
				}
		}

		private static List<MethodInfo> GetMethods(IEnumerable<Type> interfaceList)
		{
			var methods = new List<MethodInfo>();

			foreach (var interfaceType in interfaceList)
			{
				foreach (var methodInfo in interfaceType.GetMethods())
				{
					if(!methods.Contains(methodInfo))
						methods.Add(methodInfo);
				}
			}

			return methods;
		}

		private static void DefineConstructor(TypeBuilder typeBuilder)
		{
			var constructorAttributes = MethodAttributes.Public |
			                                         MethodAttributes.HideBySig | MethodAttributes.SpecialName |
			                                         MethodAttributes.RTSpecialName;

			var constructor =
				typeBuilder.DefineConstructor(constructorAttributes, CallingConventions.Standard, new Type[] { });

			var il = constructor.GetILGenerator();

			constructor.SetImplementationFlags(MethodImplAttributes.IL | MethodImplAttributes.Managed);

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Call, BaseConstructor);
			il.Emit(OpCodes.Ret);
		}
	}
}