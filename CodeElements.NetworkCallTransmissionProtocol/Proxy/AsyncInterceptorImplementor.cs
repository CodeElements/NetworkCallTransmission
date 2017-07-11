using System.Reflection;
using System.Reflection.Emit;

namespace CodeElements.NetworkCallTransmissionProtocol.Proxy
{
    internal class AsyncInterceptorImplementor
    {
        public FieldBuilder InterceptorField { get; private set; }
        public FieldBuilder MethodsField { get; private set; }

        public void ImplementProxy(TypeBuilder typeBuilder)
        {
            // Implement the IAsyncInterceptorProxy interface
            typeBuilder.AddInterfaceImplementation(typeof(IAsyncInterceptorProxy));

            MethodsField = ImplementorHelper.ImplementProperty(typeBuilder, "Methods", typeof(MethodInfo[]),
                typeof(IAsyncInterceptorProxy));

            InterceptorField = ImplementorHelper.ImplementProperty(typeBuilder, "Interceptor",
                typeof(IAsyncInterceptor),
                typeof(IAsyncInterceptorProxy));
        }
    }
}