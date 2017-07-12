using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace CodeElements.NetworkCallTransmissionProtocol.Test
{
    public class CallTransmissionExceptionTests : CallTransmissionTestBase<IExceptionTestInterface>
    {
        public CallTransmissionExceptionTests() : base(new ExceptionTestInterface())
        {
        }

        [Fact]
        public Task TestNotImplementedException()
        {
            return Assert.ThrowsAsync<NotImplementedException>(
                async () => await CallTransmission.Interface.Test());
        }

        [Fact]
        public async Task TestWebException()
        {
            var exception =
                await Assert.ThrowsAsync<WebException>(async () => await CallTransmission.Interface.Test2());
            Assert.Equal("234adasd", exception.Message);
            Assert.NotNull(exception.StackTrace);
        }

        [Fact]
        public async Task TestArgumentException()
        {
            var exception =
                await Assert.ThrowsAsync<ArgumentException>(async () => await CallTransmission.Interface.Test3("asd"));
            Assert.StartsWith("This is a test", exception.Message);
            Assert.Equal("asd", exception.ParamName);
            Assert.NotNull(exception.StackTrace);
        }

        [Fact]
        public async Task TestRemoteCallException()
        {
            var exception =
                await Assert.ThrowsAsync<RemoteCallException>(async () => await CallTransmission.Interface.Test4(1));
            Assert.Equal(typeof(AggregateException).FullName, exception.ExceptionType);
            Assert.NotNull(exception.StackTrace);
        }
    }

    public class ExceptionTestInterface : IExceptionTestInterface
    {
        public Task Test()
        {
            throw new NotImplementedException();
        }

        public Task<string> Test2()
        {
            throw new WebException("234adasd", WebExceptionStatus.ConnectFailure);
        }

        public Task<bool> Test3(string asd)
        {
            throw new ArgumentException("This is a test", nameof(asd));
        }

        public Task Test4(byte asd)
        {
            throw new RemoteCallException(new AggregateException());
        }
    }

    public interface IExceptionTestInterface
    {
        Task Test();
        Task<string> Test2();
        Task<bool> Test3(string asd);
        Task Test4(byte asd);
    }
}