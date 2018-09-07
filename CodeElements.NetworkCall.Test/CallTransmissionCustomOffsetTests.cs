using System;
using System.Threading.Tasks;
using Xunit;

namespace CodeElements.NetworkCall.Test
{
    public class CallTransmissionCustomOffsetTests : NetworkCallTestBase<IOffsetTestInterface>
    {
        private const int ProtocolOffset = 3241;
        private const int ExecuterOffset = 3232;

        public CallTransmissionCustomOffsetTests() : base(new OffsetTestInterfaceImplementation())
        {
            Client.CustomOffset = ProtocolOffset;
            Server.CustomOffset = ExecuterOffset;
        }

        protected override Task SendData(BufferSegment data, DataTransmitter target)
        {
            using (data)
            {
                var nukeLength = target == Client ? ExecuterOffset : ProtocolOffset;

                Buffer.BlockCopy(new byte[nukeLength], 0, data.Buffer, 0, nukeLength); //null all offset bytes
                target.ReceiveData(data.Buffer, data.Offset);
            }

            return Task.CompletedTask;
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
            var result = await Client.Interface.Test("632D923B-3303-4AEE-B96B-2AAB8525F139");
            Assert.Equal("861CE16C-98F6-4BB7-B30C-821A78FDF1C4", result);
        }
    }
}