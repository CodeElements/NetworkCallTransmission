using System;
using Castle.DynamicProxy;

namespace CodeElements.NetworkCallTransmissionProtocol.Castle
{
    internal class CastleProxyFactory
    {
        private static readonly ProxyGenerator Generator = CreateProxyGenerator();

        public static object CreateProxy(Type interfaceType, IAsyncInterceptor asyncInterceptor)
        {
            var options = new ProxyGenerationOptions(new ProxyMethodHook());

            return Generator.CreateClassProxy(typeof(object), new[] {interfaceType}, options, asyncInterceptor);
        }

        private static ProxyGenerator CreateProxyGenerator()
        {
            return new ProxyGenerator();
        }
    }
}