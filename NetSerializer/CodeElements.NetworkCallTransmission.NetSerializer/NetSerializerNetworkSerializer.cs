using System;
using CodeElements.NetworkCallTransmission.ExceptionWrapping;
using NetSerializer;

namespace CodeElements.NetworkCallTransmission.NetSerializer
{
    public class NetSerializerNetworkSerializer : INetworkCallSerializer
    {
        public static NetSerializerNetworkSerializer Instance = new NetSerializerNetworkSerializer();
        private readonly SerializerCache _serializerCache;

        public NetSerializerNetworkSerializer()
        {
            _serializerCache = new SerializerCache();
        }

        public object Deserialize(Type type, byte[] data, int offset)
        {
            return _serializerCache.GetSerializer(type).Deserialize(data, offset);
        }

        public Exception DeserializeException(byte[] data, int offset)
        {
            return ExceptionInfo.ExceptionWrapperSerializer.Deserialize<ExceptionInfo>(data, offset).GetException();
        }

        public int Serialize(Type type, ref byte[] buffer, int offset, object value)
        {
            return Serialize(_serializerCache.GetSerializer(type), ref buffer, offset, value);
        }

        public int SerializeException(ref byte[] buffer, int offset, Exception exception)
        {
            var exceptionInfo = ExceptionFactory.PackException(exception);
            return Serialize(ExceptionInfo.ExceptionWrapperSerializer, ref buffer, offset, exceptionInfo);
        }

        private static int Serialize(Serializer serializer, ref byte[] buffer, int offset, object value)
        {
            using (var stream = new ResizableMemoryStream(buffer, offset))
            {
                stream.Position = offset;
                serializer.Serialize(stream, value);
                buffer = stream.Data;
                return (int) stream.Position - offset;
            }
        }
    }
}