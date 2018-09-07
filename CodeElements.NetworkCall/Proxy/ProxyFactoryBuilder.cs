using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeElements.NetworkCall.Proxy
{
    internal class ProxyFactoryBuilder
    {
        private static readonly ConstructorInfo BaseConstructor =
            typeof(object).GetTypeInfo().GetConstructor(new Type[0]);

        private readonly List<Type> _interfaceList;

        private readonly Type _interfaceType;
        private readonly TypeBuilder _typeBuilder;
        private IReadOnlyList<EventInfo> _interceptedEvents;

        private IReadOnlyList<MethodInfo> _interceptedMethods;

        internal ProxyFactoryBuilder(Type interfaceType)
        {
            _interfaceType = interfaceType;
            _typeBuilder = BuildTypeFromInterface(interfaceType, out _interfaceList);
        }

        public ProxyFactory Build() => new ProxyFactory(_typeBuilder.CreateTypeInfo(), _interceptedMethods, _interceptedEvents);

        public IReadOnlyList<MethodInfo> InterceptMethods()
        {
            //Implement IAsyncInterceptorProxy
            var implementor = new AsyncInterceptorImplementor();
            implementor.ImplementProxy(_typeBuilder);

            var methods = GetMethods(_interfaceList);
            var builder = new ProxyMethodBuilder();

            for (var i = 0; i < methods.Count; i++)
                builder.CreateMethod(implementor.InterceptorField, methods[i], i, _typeBuilder);

            _interceptedMethods = methods;
            return methods;
        }

        public IReadOnlyList<EventInfo> InterceptEvents()
        {
            var eventInterceptorImplementor = new EventInterceptorImplementor();
            eventInterceptorImplementor.ImplementProxy(_typeBuilder);

            var events = GetEvents(_interfaceList);
            foreach (var eventInfo in events)
            {
                if (!eventInfo.EventHandlerType.IsGenericType)
                    if (eventInfo.EventHandlerType != typeof(EventHandler))
                        throw new ArgumentException("Only EventHandler and EventHandler<> are allowed");
                    else
                        continue;

                var type = eventInfo.EventHandlerType.GetGenericTypeDefinition();
                if (!(type == typeof(EventHandler<>)))
                    throw new ArgumentException("Only EventHandler and EventHandler<> are allowed");
            }

            var eventFields = new FieldBuilder[events.Count];

            var builder = new ProxyEventBuilder();
            for (var index = 0; index < events.Count; index++)
            {
                var eventInfo = events[index];
                eventFields[index] = builder.CreateEvent(index, _interfaceType,
                    eventInterceptorImplementor.InterceptorField, eventInfo, _typeBuilder);
            }

            eventInterceptorImplementor.ImplementTriggerEvent(_typeBuilder, eventFields, events);

            _interceptedEvents = events;
            return events;
        }

        private static List<MethodInfo> GetMethods(IEnumerable<Type> interfaceList)
        {
            var methods = new List<MethodInfo>();

            foreach (var interfaceType in interfaceList)
            foreach (var methodInfo in interfaceType.GetMethods().Where(x => !x.IsSpecialName))
                if (!methods.Contains(methodInfo))
                    methods.Add(methodInfo);

            return methods;
        }

        private static List<EventInfo> GetEvents(IEnumerable<Type> interfaceList)
        {
            var events = new List<EventInfo>();

            foreach (var interfaceType in interfaceList)
            foreach (var eventInfo in interfaceType.GetEvents())
                if (!events.Contains(eventInfo))
                    events.Add(eventInfo);

            return events;
        }

        private static TypeBuilder BuildTypeFromInterface(Type interfaceType, out List<Type> interfaceList)
        {
            var typeName = $"{interfaceType.Name}Proxy";
            var assemblyName = $"{typeName}Assembly";
            var moduleName = $"{typeName}Module";

            var name = new AssemblyName(assemblyName);

            var access = AssemblyBuilderAccess.Run;
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, access);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName);
            var typeAttributes = TypeAttributes.AutoClass | TypeAttributes.Class | TypeAttributes.Public;

            interfaceList = new List<Type> {interfaceType};
            BuildInterfaceList(interfaceType, interfaceList);

            var typeBuilder = moduleBuilder.DefineType(typeName, typeAttributes, typeof(object),
                interfaceList.Select(x => x).ToArray());

            DefineConstructor(typeBuilder);

            return typeBuilder;
        }

        private static void BuildInterfaceList(Type currentType, ICollection<Type> interfaceTypes)
        {
            var interfaces = currentType.GetInterfaces();
            if (interfaces.Length > 0)
                foreach (var type in interfaces)
                {
                    var typeInfo = type.GetTypeInfo();

                    if (interfaceTypes.Contains(typeInfo))
                        continue;

                    interfaceTypes.Add(typeInfo);
                    BuildInterfaceList(typeInfo, interfaceTypes);
                }
        }

        private static void DefineConstructor(TypeBuilder typeBuilder)
        {
            var constructorAttributes = MethodAttributes.Public | MethodAttributes.HideBySig |
                                        MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

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