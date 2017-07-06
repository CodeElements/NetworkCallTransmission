using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace CodeElements.NetworkCallTransmissionProtocol.Test
{
    public class CallTransmissionExecuterCacheTests
    {
        private readonly CallTransmissionExecuter<ITestInterface> _executer;
        private readonly CallTransmissionProtocol<ITestInterface> _transmissionProtocol;

        public CallTransmissionExecuterCacheTests()
        {
            var exe1 = new CallTransmissionExecuter<ITestInterface>(new TestInterfaceImpl());
            var cache = exe1.Cache;
            _executer = new CallTransmissionExecuter<ITestInterface>(new TestInterfaceImpl(), cache);

            _transmissionProtocol = new CallTransmissionProtocol<ITestInterface>
            {
                SendData = SendData,
                WaitTimeout = TimeSpan.FromSeconds(5)
            };
        }

        private async Task SendData(MemoryStream memoryStream)
        {
            var buffer = memoryStream.ToArray();
            var result = await _executer.ReceiveData(buffer, 0, buffer.Length);
            _transmissionProtocol.ReceiveData(result, 0, result.Length);
        }

        [Fact]
        public async Task TestSum()
        {
            Assert.Equal(5, await _transmissionProtocol.Interface.SumValues(2, 3));
        }
    }
}