using System;
using System.IO;
using System.Threading.Tasks;
using CodeElements.NetworkCallTransmissionProtocol.Internal;
using Xunit;

namespace CodeElements.NetworkCallTransmissionProtocol.Test
{
    public class ResultCallbackTests
    {
        [Fact]
        public void TestDisposeAfterCreating()
        {
            var x = new ResultCallback();
            x.Dispose();
        }

        [Fact]
        public async Task TestWaitTimeout()
        {
            var x = new ResultCallback();
           Assert.False(await x.Wait(TimeSpan.FromMilliseconds(200)));
        }

        [Fact]
        public async Task TestAwaitWorks()
        {
            var testData = new byte[12];
            StaticRandom.NextBytes(testData);

            var x = new ResultCallback();
            await Task.Delay(TimeSpan.FromMilliseconds(50))
                .ContinueWith(task => x.ReceivedResult(ResponseType.MethodExecuted, testData, 4));
            await x.Wait(TimeSpan.FromSeconds(1));

            Assert.Equal(ResponseType.MethodExecuted, x.ResponseType);
            Assert.Equal(testData, x.Data);
            Assert.Equal(4, x.Offset);
        }

        [Fact]
        public async Task TestAwaitTimeoutAndReceivedResult()
        {
            var x = new ResultCallback();
            Assert.False(await x.Wait(TimeSpan.FromMilliseconds(100)));
            x.ReceivedResult(ResponseType.Exception, null, 0); //no exception
        }

        [Fact]
        public async Task TestReceivedResultAfterDispose()
        {
            var x = new ResultCallback();
            Assert.False(await x.Wait(TimeSpan.FromMilliseconds(50)));
            x.Dispose();
            x.ReceivedResult(ResponseType.MethodExecuted, null, 0);
        }
    }
}