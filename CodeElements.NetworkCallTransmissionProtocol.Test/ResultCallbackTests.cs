using System;
using System.IO;
using System.Threading.Tasks;
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
            var x = new ResultCallback();
            var testMemoryStream = new MemoryStream(new byte[1]);
            await Task.Delay(TimeSpan.FromMilliseconds(50))
                .ContinueWith(task => x.ReceivedResult(ResponseType.MethodExecuted, testMemoryStream));
            await x.Wait(TimeSpan.FromSeconds(1));

            Assert.True(x.ResponseType == ResponseType.MethodExecuted);
            Assert.True(x.Data == testMemoryStream);
        }

        [Fact]
        public async Task TestAwaitTimeoutAndReceivedResult()
        {
            var x = new ResultCallback();
            Assert.False(await x.Wait(TimeSpan.FromMilliseconds(100)));
            x.ReceivedResult(ResponseType.Exception, null); //no exception
        }

        [Fact]
        public async Task TestReceivedResultAfterDispose()
        {
            var x = new ResultCallback();
            Assert.False(await x.Wait(TimeSpan.FromMilliseconds(50)));
            x.Dispose();
            x.ReceivedResult(ResponseType.MethodExecuted, null);
        }
    }
}