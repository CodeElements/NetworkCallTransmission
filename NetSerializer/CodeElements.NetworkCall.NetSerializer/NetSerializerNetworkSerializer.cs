using System;
using System.Buffers;
using CodeElements.NetworkCallTransmission.ExceptionWrapping;
using NetSerializer;

namespace CodeElements.NetworkCall.NetSerializer
{
    public class NetSerializerNetworkSerializer : INetworkSerializer
    {
        private readonly SerializerCache _serializerCache;

        public NetSerializerNetworkSerializer()
        {
            _serializerCache = new SerializerCache();
        }

        public object Deserialize(Type type, byte[] data, int offset) =>
            _serializerCache.GetSerializer(type).Deserialize(data, offset);

        public int Serialize(Type type, ref byte[] buffer, int offset, object value, ArrayPool<byte> pool) =>
            Serialize(_serializerCache.GetSerializer(type), ref buffer, offset, value, pool);

        public Exception DeserializeException(byte[] data, int offset) =>
            ExceptionInfo.ExceptionWrapperSerializer.Deserialize<ExceptionInfo>(data, offset).GetException();

        public int SerializeException(ref byte[] buffer, int offset, Exception exception, ArrayPool<byte> pool)
        {
            var exceptionInfo = ExceptionFactory.PackException(exception);
            return Serialize(ExceptionInfo.ExceptionWrapperSerializer, ref buffer, offset, exceptionInfo, pool);
        }

        public int Serialize(Serializer serializer, ref byte[] buffer, int offset, object value, ArrayPool<byte> pool)
        {
            var poolStream = new PooledMemoryStream(buffer, offset, pool); //do not dispose
            serializer.Serialize(poolStream, value);

            var newBuffer = poolStream.ToUnsafeArraySegment();
            buffer = newBuffer.Array;
            return newBuffer.Count;
        }
    }
}