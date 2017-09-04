using System;
using System.Threading.Tasks;
using Xunit;

namespace CodeElements.NetworkCallTransmission.Test
{
    public class CallTransmissionExecuterCacheTests
    {
        public CallTransmissionExecuterCacheTests()
        {
            var exe1 = new CallTransmissionExecuter<IBasicTestInterface>(new BasicTestInterfaceImpl(),
                Properties.Serializer);
            var cache = exe1.Cache;
            _executer = new CallTransmissionExecuter<IBasicTestInterface>(new BasicTestInterfaceImpl(),
                Properties.Serializer, cache);

            _transmission = new CallTransmission<IBasicTestInterface>(Properties.Serializer)
            {
                SendData = SendData,
                WaitTimeout = TimeSpan.FromSeconds(5)
            };
        }

        private readonly CallTransmissionExecuter<IBasicTestInterface> _executer;
        private readonly CallTransmission<IBasicTestInterface> _transmission;

        private async Task SendData(ArraySegment<byte> responseData)
        {
            var buffer = responseData;
            using (var result = await _executer.ReceiveData(buffer.Array, 0))
                _transmission.ReceiveData(result.Buffer, 0);
        }

        [Fact]
        public async Task TestSum()
        {
            Assert.Equal(5, await _transmission.Interface.SumValues(2, 3));
        }
    }
}