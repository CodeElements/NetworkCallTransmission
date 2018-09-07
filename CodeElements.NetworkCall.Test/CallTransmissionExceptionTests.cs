using System;
using System.Threading.Tasks;
using Xunit;

namespace CodeElements.NetworkCall.Test
{
    public class CallTransmissionExceptionTests : NetworkCallTestBase<IExceptionTestInterface>
    {
        public CallTransmissionExceptionTests() : base(new ExceptionTestInterface())
        {
        }

        [Fact]
        public Task TestNotImplementedException()
        {
            return Assert.ThrowsAsync<NotImplementedException>(
                async () => await Client.Interface.Test());
        }

        [Fact]
        public async Task TestWebException()
        {
            var exception =
                await Assert.ThrowsAsync<InvalidCastException>(async () => await Client.Interface.Test2());
            Assert.Equal("234adasd", exception.Message);
            Assert.NotNull(exception.StackTrace);
        }

        [Fact]
        public async Task TestArgumentException()
        {
            var exception =
                await Assert.ThrowsAsync<ArgumentException>(async () => await Client.Interface.Test3("asd"));
            Assert.StartsWith("This is a test", exception.Message);
            Assert.Equal("asd", exception.ParamName);
            Assert.NotNull(exception.StackTrace);
        }

        [Fact]
        public async Task TestAggregateException()
        {
            var exception =
                await Assert.ThrowsAsync<AggregateException>(async () => await Client.Interface.Test4(1));
            Assert.IsType<ObjectDisposedException>(exception.InnerExceptions[0]);
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
            throw new InvalidCastException("234adasd", 123);
        }

        public Task<bool> Test3(string asd)
        {
            throw new ArgumentException("This is a test", nameof(asd));
        }

        public Task Test4(byte asd)
        {
            throw new AggregateException(new ObjectDisposedException("SslStream"));
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