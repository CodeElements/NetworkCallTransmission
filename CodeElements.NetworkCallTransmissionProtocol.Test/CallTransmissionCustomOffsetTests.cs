using System;
using System.Threading.Tasks;
using Xunit;

namespace CodeElements.NetworkCallTransmissionProtocol.Test
{
    public class CallTransmissionCustomOffsetTests : CallTransmissionTestBase<IOffsetTestInterface>
    {
        private const int ProtocolOffset = 3241;
        private const int ExecuterOffset = 3232;

        public CallTransmissionCustomOffsetTests() : base(new OffsetTestInterfaceImplementation())
        {
            CallTransmissionProtocol.CustomOffset = ProtocolOffset;
            CallTransmissionExecuter.CustomOffset = ExecuterOffset;
        }

        protected override async Task SendData(ResponseData data)
        {
            var buffer = data.Data;
            Buffer.BlockCopy(new byte[ProtocolOffset], 0, buffer, 0, ProtocolOffset); //null all offset bytes

            var result = await CallTransmissionExecuter.ReceiveData(buffer, ProtocolOffset, data.Length);
            Buffer.BlockCopy(new byte[ExecuterOffset], 0, result.Data, 0, ExecuterOffset);

            CallTransmissionProtocol.ReceiveData(result.Data, ExecuterOffset, result.Length);
        }

        private class OffsetTestInterfaceImplementation : IOffsetTestInterface
        {
            public Task<string> Test(string test)
            {
                Assert.Equal("632D923B-3303-4AEE-B96B-2AAB8525F139", test);
                return Task.FromResult("861CE16C-98F6-4BB7-B30C-821A78FDF1C4");
            }
        }

        [Fact]
        public async Task TestSendReceive()
        {
            var result = await CallTransmissionProtocol.Interface.Test("632D923B-3303-4AEE-B96B-2AAB8525F139");
            Assert.Equal("861CE16C-98F6-4BB7-B30C-821A78FDF1C4", result);
        }
    }
}