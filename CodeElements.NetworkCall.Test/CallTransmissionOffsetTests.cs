using System;
using System.Threading.Tasks;
using Xunit;

namespace CodeElements.NetworkCall.Test
{
    public class CallTransmissionOffsetTests : NetworkCallTestBase<IOffsetTestInterface>
    {
        private const int Offset = 23421;

        public CallTransmissionOffsetTests() : base(new OffsetTestInterfaceImplementation())
        {
        }

        protected override Task SendData(BufferSegment data, DataTransmitter target)
        {
            var buffer = new byte[data.Length + Offset];
            Buffer.BlockCopy(data.Buffer, 0, buffer, Offset, data.Length);

            target.ReceiveData(buffer, Offset);
            return Task.CompletedTask;
        }

        [Fact]
        public async Task TestSendReceive()
        {
            var result = await Client.Interface.Test("43F0246C-3943-4D02-A891-61AC2EE152E2");
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