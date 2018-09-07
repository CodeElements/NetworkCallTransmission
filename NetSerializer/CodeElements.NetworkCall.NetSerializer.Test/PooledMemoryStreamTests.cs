using System;
using System.Buffers;
using Xunit;

namespace CodeElements.NetworkCall.NetSerializer.Test
{
    public class PooledMemoryStreamTests
    {
        [Fact]
        public void TestWriteData()
        {
            using (var ms = new PooledMemoryStream())
            {
                var testData = new byte[4096];
                new Random().NextBytes(testData);

                ms.Write(testData, 0, testData.Length);

                var result = ms.ToUnsafeArraySegment();
                Assert.Equal(0, result.Offset);
                Assert.Equal(4096, result.Count);
                Assert.Equal(new ArraySegment<byte>(testData), result);
            }
        }

        [Fact]
        public void TestWriteDataToExistingBuffer()
        {
            var buffer = ArrayPool<byte>.Shared.Rent(4096);

            using (var ms = new PooledMemoryStream(buffer, 20, ArrayPool<byte>.Shared))
            {
                var testData = new byte[2048];
                new Random().NextBytes(testData);

                ms.Write(testData, 0, testData.Length);

                var result = ms.ToUnsafeArraySegment();
                Assert.Equal(20, result.Offset);
                Assert.Equal(2048, result.Count);
                Assert.Equal(new ArraySegment<byte>(testData), result);
            }
        }
    }
}