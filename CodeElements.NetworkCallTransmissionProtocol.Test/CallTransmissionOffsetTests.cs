using System;
using System.Threading.Tasks;
using Xunit;

namespace CodeElements.NetworkCallTransmissionProtocol.Test
{
    public class CallTransmissionOffsetTests : CallTransmissionTestBase<IOffsetTestInterface>
    {
        private const int Offset = 23421;

        public CallTransmissionOffsetTests() : base(new OffsetTestInterfaceImplementation())
        {
        }

        protected override async Task SendData(ResponseData data)
        {
            var buffer = new byte[data.Length + Offset];
            Buffer.BlockCopy(data.Data, 0, buffer, Offset, data.Length);

            var result = await CallTransmissionExecuter.ReceiveData(buffer, Offset, data.Length);

            var responseBuffer = new byte[result.Length + Offset];
            Buffer.BlockCopy(result.Data, 0, responseBuffer, Offset, result.Length);

            CallTransmissionProtocol.ReceiveData(responseBuffer, Offset, result.Length);
        }

        [Fact]
        public async Task TestSendReceive()
        {
            var result = await CallTransmissionProtocol.Interface.Test("43F0246C-3943-4D02-A891-61AC2EE152E2");
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