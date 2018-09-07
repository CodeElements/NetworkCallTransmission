using CodeElements.NetworkCall.Internal;

namespace CodeElements.NetworkCall.Proxy
{
    internal static class CachedProxyFactory<TInterface>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly ProxyFactory ProxyFactory;

        static CachedProxyFactory()
        {
            var proxyBuilder = new ProxyFactoryBuilder(typeof(TInterface));
            proxyBuilder.InterceptEvents();
            proxyBuilder.InterceptMethods();

            ProxyFactory = proxyBuilder.Build();
        }

        public static TInterface Create(IAsyncInterceptor asyncInterceptor, IEventInterceptor eventInterceptor) =>
            ProxyFactory.Create<TInterface>(asyncInterceptor, eventInterceptor);
    }
}