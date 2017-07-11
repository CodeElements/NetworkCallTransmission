using System.Reflection;
using System.Reflection.Emit;

namespace CodeElements.NetworkCallTransmissionProtocol.Proxy
{
    internal class EventInterceptorImplementor
    {
        public FieldBuilder InterceptorField { get; private set; }
        public FieldBuilder EventsField { get; private set; }

        public void ImplementProxy(TypeBuilder typeBuilder)
        {
            // Implement the IAsyncInterceptorProxy interface
            typeBuilder.AddInterfaceImplementation(typeof(IEventInterceptorProxy));

            InterceptorField = ImplementorHelper.ImplementProperty(typeBuilder,
                nameof(IEventInterceptorProxy.Interceptor),
                typeof(IEventInterceptor), typeof(IEventInterceptorProxy));

            EventsField = ImplementorHelper.ImplementProperty(typeBuilder, nameof(IEventInterceptorProxy.Events),
                typeof(EventInfo[]), typeof(IEventInterceptorProxy));
        }
    }
}