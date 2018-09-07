using CodeElements.NetworkCall.NetSerializer;

namespace CodeElements.NetworkCall.Test
{
    public static class Properties
    {
        public static INetworkSerializer Serializer = new NetSerializerNetworkSerializer();
    }
}