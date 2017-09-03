using System;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeElements.NetworkCallTransmission.Proxy
{
    public static class DisposeProxyBuilder
    {
        public static void BuildProxy(TypeBuilder typeBuilder, MethodInfo getInterceptorField, MethodInfo disposeProxyMethod)
        {
            var methodAttributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig;
            var methodBuilder = typeBuilder.DefineMethod(nameof(IDisposable.Dispose), methodAttributes,
                CallingConventions.HasThis, typeof(void), null);

            var il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, getInterceptorField);
            il.Emit(OpCodes.Callvirt, disposeProxyMethod);
            il.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, typeof(IDisposable).GetTypeInfo().GetMethod(nameof(IDisposable.Dispose)));
        }
    }
}