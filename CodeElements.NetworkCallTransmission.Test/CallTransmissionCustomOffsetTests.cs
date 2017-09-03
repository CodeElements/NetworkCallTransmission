using System;
using System.Threading.Tasks;
using Xunit;

namespace CodeElements.NetworkCallTransmission.Test
{
    public class CallTransmissionCustomOffsetTests : CallTransmissionTestBase<IOffsetTestInterface>
    {
        private const int ProtocolOffset = 3241;
        private const int ExecuterOffset = 3232;

        public CallTransmissionCustomOffsetTests() : base(new OffsetTestInterfaceImplementation())
        {
            CallTransmission.CustomOffset = ProtocolOffset;
            CallTransmissionExecuter.CustomOffset = ExecuterOffset;
        }

        protected override async Task SendData(ArraySegment<byte> data)
        {
            var buffer = data.Array;
            Buffer.BlockCopy(new byte[ProtocolOffset], 0, buffer, 0, ProtocolOffset); //null all offset bytes

            using (var result = await CallTransmissionExecuter.ReceiveData(buffer, ProtocolOffset))
            {
                Buffer.BlockCopy(new byte[ExecuterOffset], 0, result.Buffer, 0, ExecuterOffset);

                CallTransmission.ReceiveData(result.Buffer, ExecuterOffset);
            }
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
            var result = await CallTransmission.Interface.Test("632D923B-3303-4AEE-B96B-2AAB8525F139");
            Assert.Equal("861CE16C-98F6-4BB7-B30C-821A78FDF1C4", result);
        }
    }
}