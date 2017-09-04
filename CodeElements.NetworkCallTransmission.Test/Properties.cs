using CodeElements.NetworkCallTransmission.ZeroFormatter;

namespace CodeElements.NetworkCallTransmission.Test
{
    public static class Properties
    {
        public static INetworkCallSerializer Serializer = ZeroFormatterNetworkSerializer.Instance;
    }
}