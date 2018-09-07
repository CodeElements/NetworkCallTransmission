using System;
using System.Buffers;
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

        public object Deserialize(Type type, byte[] data, int offset)
        {
            return _serializerCache.GetSerializer(type).Deserialize(data, offset);
        }

        public int Serialize(Type type, ref byte[] buffer, int offset, object value, ArrayPool<byte> pool)
        {
            var poolStream = new PooledMemoryStream(buffer, offset, pool); //do not dispose
            _serializerCache.GetSerializer(type).Serialize(poolStream, value);

            var newBuffer = poolStream.ToUnsafeArraySegment();
            buffer = newBuffer.Array;
            return newBuffer.Count;
        }

        public Exception DeserializeException(byte[] data, int offset) => throw new NotImplementedException();

        public int SerializeException(ref byte[] buffer, int offset, Exception exception, ArrayPool<byte> pool) => throw new NotImplementedException();
    }
}
