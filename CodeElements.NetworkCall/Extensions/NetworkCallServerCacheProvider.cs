namespace CodeElements.NetworkCall.Extensions
{
    public static class NetworkCallServerCacheProvider<TInterface>
    {
        static NetworkCallServerCacheProvider()
        {
            Cache = NetworkCallServerCache.Build<TInterface>();
        }

        public static NetworkCallServerCache Cache { get; }
    }
}