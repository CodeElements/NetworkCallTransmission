using System;
using System.Threading.Tasks;
using CodeElements.NetworkCallTransmissionProtocol.Internal;
using Xunit;

namespace CodeElements.NetworkCallTransmissionProtocol.Test.Internal
{
    public class ResultCallbackTests
    {
        [Fact]
        public async Task TestAwaitTimeoutAndReceivedResult()
        {
            var x = new ResultCallback();
            Assert.False(await x.Wait(TimeSpan.FromMilliseconds(100)));
            x.ReceivedResult(CallTransmissionResponseType.Exception, null, 0); //no exception
        }

        [Fact]
        public async Task TestAwaitWorks()
        {
            var testData = new byte[12];
            StaticRandom.NextBytes(testData);

            var x = new ResultCallback();
            await Task.Delay(TimeSpan.FromMilliseconds(50))
                .ContinueWith(task => x.ReceivedResult(CallTransmissionResponseType.MethodExecuted, testData, 4));
            await x.Wait(TimeSpan.FromSeconds(1));

            Assert.Equal(CallTransmissionResponseType.MethodExecuted, x.ResponseType);
            Assert.Equal(testData, x.Data);
            Assert.Equal(4, x.Offset);
        }

        [Fact]
        public void TestDisposeAfterCreating()
        {
            var x = new ResultCallback();
            x.Dispose();
        }

        [Fact]
        public async Task TestReceivedResultAfterDispose()
        {
            var x = new ResultCallback();
            Assert.False(await x.Wait(TimeSpan.FromMilliseconds(50)));
            x.Dispose();
            x.ReceivedResult(CallTransmissionResponseType.MethodExecuted, null, 0);
        }

        [Fact]
        public async Task TestWaitTimeout()
        {
            var x = new ResultCallback();
            Assert.False(await x.Wait(TimeSpan.FromMilliseconds(200)));
        }
    }
}