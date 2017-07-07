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

            _transmissionProtocol = new CallTransmissionProtocol<IBasicTestInterface>
            {
                SendData = SendData,
                WaitTimeout = TimeSpan.FromSeconds(5)
            };
        }

        private readonly CallTransmissionExecuter<IBasicTestInterface> _executer;
        private readonly CallTransmissionProtocol<IBasicTestInterface> _transmissionProtocol;

        private async Task SendData(ResponseData responseData)
        {
            var buffer = responseData;
            var result = await _executer.ReceiveData(buffer.Data, 0, buffer.Length);
            _transmissionProtocol.ReceiveData(result.Data, 0, result.Length);
        }

        [Fact]
        public async Task TestSum()
        {
            Assert.Equal(5, await _transmissionProtocol.Interface.SumValues(2, 3));
        }
    }
}