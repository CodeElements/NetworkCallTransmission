using System;
using System.Threading.Tasks;
using Xunit;

namespace CodeElements.NetworkCallTransmission.Test
{
    public class CallTransmissionOffsetTests : CallTransmissionTestBase<IOffsetTestInterface>
    {
        private const int Offset = 23421;

        public CallTransmissionOffsetTests() : base(new OffsetTestInterfaceImplementation())
        {
        }

        protected override async Task SendData(ArraySegment<byte> data)
        {
            var buffer = new byte[data.Count + Offset];
            Buffer.BlockCopy(data.Array, 0, buffer, Offset, data.Count);

            var result = await CallTransmissionExecuter.ReceiveData(buffer, Offset);

            var responseBuffer = new byte[result.Count + Offset];
            Buffer.BlockCopy(result.Array, 0, responseBuffer, Offset, result.Count);

            CallTransmission.ReceiveData(responseBuffer, Offset);
        }

        [Fact]
        public async Task TestSendReceive()
        {
            var result = await CallTransmission.Interface.Test("43F0246C-3943-4D02-A891-61AC2EE152E2");
            Assert.Equal("19E27D75-E32B-4F47-85F9-095A1025BE2E", result);
        }

        private class OffsetTestInterfaceImplementation : IOffsetTestInterface
        {
            public Task<string> Test(string test)
            {
                Assert.Equal("43F0246C-3943-4D02-A891-61AC2EE152E2", test);
                return Task.FromResult("19E27D75-E32B-4F47-85F9-095A1025BE2E");
            }
        }
    }

    public interface IOffsetTestInterface
    {
        Task<string> Test(string test);
    }
}