using System;
using System.Buffers;
using Xunit;

namespace CodeElements.NetworkCall.NetSerializer.Test
{
    public class NetSerializerTests
    {
        private INetworkSerializer _networkSerializer;

        public NetSerializerTests()
        {
            _networkSerializer = new NetSerializerNetworkSerializer();
        }

        [Fact]
        public void TestSerializeString()
        {
            var buffer = ArrayPool<byte>.Shared.Rent(16);
            var count = _networkSerializer.Serialize(typeof(string), ref buffer, 4, "Hello World",
                ArrayPool<byte>.Shared);

            Assert.Equal(new ArraySegment<byte>(new byte[4]), new ArraySegment<byte>(buffer, 0, 4));
            Assert.Equal(14, count);

            var result = _networkSerializer.Deserialize(typeof(string), buffer, 4);
            Assert.Equal("Hello World", (string) result);
        }
    }
}