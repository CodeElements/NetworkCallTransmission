using System;
using System.IO;
using NetSerializer;

namespace CodeElements.NetworkCallTransmission.NetSerializer
{
    public class NetSerializerNetworkSerializer : INetworkCallSerializer
    {
        private readonly SerializerCache _serializerCache;

        public static NetSerializerNetworkSerializer Instance = new NetSerializerNetworkSerializer();

        public NetSerializerNetworkSerializer()
        {
            _serializerCache = new SerializerCache();
        }

        public object Deserialize(Type type, byte[] data, int offset)
        {
            using (var stream = new MemoryStream(data, offset, data.Length - offset, false))
                return _serializerCache.GetSerializer(type).Deserialize(stream);
        }

        public Exception DeserializeException(byte[] data, int offset)
        {
            throw new NotImplementedException();
        }

        public int Serialize(Type type, ref byte[] buffer, int offset, object value)
        {
            using (var stream = new ResizableMemoryStream(buffer, offset))
            {
                stream.Position = offset;
                _serializerCache.GetSerializer(type).Serialize(stream, value);
                buffer = stream.Data;
                return (int) stream.Position - offset;
            }
        }

        public int SerializeException(ref byte[] buffer, int offset, Exception exception)
        {
            throw new NotImplementedException();
        }
    }
}