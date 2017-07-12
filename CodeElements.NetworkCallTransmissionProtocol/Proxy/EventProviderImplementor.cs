using System.Reflection;
using System.Reflection.Emit;

namespace CodeElements.NetworkCallTransmissionProtocol.Proxy
{
    internal class EventProviderImplementor
    {
        public static void Implement(TypeBuilder typeBuilder)
        {
            // ReSharper disable once PossibleNullReferenceException
            var getInterceptorField = typeof(IEventInterceptorProxy)
                .GetProperty(nameof(IEventInterceptorProxy.Interceptor)).GetGetMethod();

            //Implement IDisponsable
            DisposeProxyBuilder.BuildProxy(typeBuilder, getInterceptorField,
                typeof(IEventInterceptor).GetMethod(nameof(IEventInterceptor.Dispose)));

            CreateProxyMethod(typeBuilder, typeof(IEventProvider).GetMethod(nameof(IEventProvider.SuspendSubscribing)),
                typeof(IEventInterceptor).GetMethod(nameof(IEventInterceptor.SuspendSubscribing)), getInterceptorField);

            CreateProxyMethod(typeBuilder, typeof(IEventProvider).GetMethod(nameof(IEventProvider.ResumeSubscribing)),
                typeof(IEventInterceptor).GetMethod(nameof(IEventInterceptor.ResumeSubscribing)), getInterceptorField);
        }

        private static void CreateProxyMethod(TypeBuilder typeBuilder, MethodInfo interfaceMethod,
            MethodInfo proxyMethod, MethodInfo getInterceptorField)
        {
            var methodAttributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig;
            var methodBuilder = typeBuilder.DefineMethod(proxyMethod.Name, methodAttributes,
                CallingConventions.HasThis);

            var il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, getInterceptorField);
            il.Emit(OpCodes.Callvirt, proxyMethod);
            il.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, interfaceMethod);
        }
    }
}