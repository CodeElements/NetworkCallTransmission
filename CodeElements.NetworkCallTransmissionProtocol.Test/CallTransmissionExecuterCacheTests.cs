using System;
using System.Threading.Tasks;
using Xunit;

namespace CodeElements.NetworkCallTransmissionProtocol.Test
{
    public class CallTransmissionExecuterCacheTests
    {
        public CallTransmissionExecuterCacheTests()
        {
            var exe1 = new CallTransmissionExecuter<IBasicTestInterface>(new BasicTestInterfaceImpl());
            var cache = exe1.Cache;
            _executer = new CallTransmissionExecuter<IBasicTestInterface>(new BasicTestInterfaceImpl(), cache);

            _transmission = new CallTransmission<IBasicTestInterface>
            {
                SendData = SendData,
                WaitTimeout = TimeSpan.FromSeconds(5)
            };
        }

        private readonly CallTransmissionExecuter<IBasicTestInterface> _executer;
        private readonly CallTransmission<IBasicTestInterface> _transmission;

        private async Task SendData(ResponseData responseData)
        {
            var buffer = responseData;
            var result = await _executer.ReceiveData(buffer.Data, 0);
            _transmission.ReceiveData(result.Data, 0);
        }

        [Fact]
        public async Task TestSum()
        {
            Assert.Equal(5, await _transmission.Interface.SumValues(2, 3));
        }
    }
}