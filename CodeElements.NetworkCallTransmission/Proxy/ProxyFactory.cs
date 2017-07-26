using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeElements.NetworkCallTransmission.Proxy
{
    internal static class ProxyFactory
	{
		private static readonly ConstructorInfo BaseConstructor = typeof(object).GetConstructor(new Type[0]);

	    public static EventProxyInitializationInfo CreateProxy<T>()
        {
	        var typeBuilder = BuildTypeFromInterface(typeof(T), out var interfaceList);

            var eventInterceptorImplementor = new EventInterceptorImplementor();
            eventInterceptorImplementor.ImplementProxy(typeBuilder);

	        var events = GetEvents(interfaceList);
            foreach (var eventInfo in events)
            {
                if (!eventInfo.EventHandlerType.IsGenericType)
                    throw new ArgumentException(
                        "Only TransmittedEventHandler<> and TransmittedEventHandler<,> are allowed");

                var type = eventInfo.EventHandlerType.GetGenericTypeDefinition();
                if (!(type == typeof(TransmittedEventHandler<>) || type == typeof(TransmittedEventHandler<,>)))
                    throw new ArgumentException(
                        "Only TransmittedEventHandler<> and TransmittedEventHandler<,> are allowed");
            }

            var eventFields = new FieldBuilder[events.Count];

            var builder = new ProxyEventBuilder();
	        for (var index = 0; index < events.Count; index++)
	        {
	            var eventInfo = events[index];
	            eventFields[index] = builder.CreateEvent(index, typeof(T), eventInterceptorImplementor.InterceptorField,
	                eventInfo, typeBuilder);
	        }

            eventInterceptorImplementor.ImplementTriggerEvent(typeBuilder, eventFields, events);

            var proxyType = typeBuilder.CreateType();
            return new EventProxyInitializationInfo(proxyType, events.ToArray());
        }

	    public static T InitializeEventProxy<T>(EventProxyInitializationInfo initializationInfo, IEventInterceptor eventInterceptor)
	    {
	        var result = (T) Activator.CreateInstance(initializationInfo.ProxyType);

	        var proxy = (IEventInterceptorProxy) result;
	        proxy.Interceptor = eventInterceptor;
	        proxy.Events = initializationInfo.Events;

	        return result;
        }

        public static T CreateProxy<T>(IAsyncInterceptor asyncInterceptor)
        {
            var typeBuilder = BuildTypeFromInterface(typeof(T), out var interfaceList);

            //Implement IAsyncInterceptorProxy
            var implementor = new AsyncInterceptorImplementor();
			implementor.ImplementProxy(typeBuilder);

			var methods = GetMethods(interfaceList);

			var builder = new ProxyMethodBuilder();
			for (var i = 0; i < methods.Count; i++)
			{
				builder.CreateMethod(implementor.InterceptorField, methods[i], i, typeBuilder);
			}

			var proxyType = typeBuilder.CreateType();
			var result = (T) Activator.CreateInstance(proxyType);

			var proxy = (IAsyncInterceptorProxy) result;
			proxy.Interceptor = asyncInterceptor;
			proxy.Methods = methods.ToArray();

			return result;
		}

        private static TypeBuilder BuildTypeFromInterface(Type interfaceType, out List<Type> interfaceList)
	    {
	        var currentDomain = AppDomain.CurrentDomain;
	        var typeName = $"{interfaceType.Name}Proxy";
	        var assemblyName = $"{typeName}Assembly";
	        var moduleName = $"{typeName}Module";

	        var name = new AssemblyName(assemblyName);

	        var access = AssemblyBuilderAccess.Run;
	        var assemblyBuilder = currentDomain.DefineDynamicAssembly(name, access);
	        var moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName);
	        var typeAttributes = TypeAttributes.AutoClass | TypeAttributes.Class |
	                             TypeAttributes.Public;

	        interfaceList = new List<Type> {interfaceType};
	        BuildInterfaceList(interfaceType, interfaceList);

	        var typeBuilder =
	            moduleBuilder.DefineType(typeName, typeAttributes, typeof(object), interfaceList.ToArray());

	        DefineConstructor(typeBuilder);

	        return typeBuilder;
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

	    private static List<EventInfo> GetEvents(IEnumerable<Type> interfaceList)
	    {
            var events = new List<EventInfo>();

	        foreach (var interfaceType in interfaceList)
	        {
	            foreach (var eventInfo in interfaceType.GetEvents())
	            {
	                if (!events.Contains(eventInfo))
	                    events.Add(eventInfo);
	            }
	        }

	        return events;
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