using CodeElements.NetworkCallTransmissionProtocol.NetSerializer;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    internal class MethodCache
    {
        public MethodCache(byte[] methodId)
        {
            MethodId = methodId;
        }

        public byte[] MethodId { get; }
        public Serializer[] ParameterSerializers { get; set; }
        public Serializer ReturnSerializer { get; set; }
    }
}