using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace CodeElements.NetworkCallTransmissionProtocol.Test
{
    public class CallTransmissionProtocolBasicTests
    {
        private readonly CallTransmissionProtocol<ITestInterface> _transmissionProtocol;
        private readonly CallTransmissionExecuter<ITestInterface> _transmissionExecuter;

        public CallTransmissionProtocolBasicTests()
        {
            _transmissionProtocol = new CallTransmissionProtocol<ITestInterface>
            {
                SendData = SendData,
                WaitTimeout = TimeSpan.FromSeconds(5)
            };
            _transmissionExecuter = new CallTransmissionExecuter<ITestInterface>(new TestInterfaceImpl());
        }

        private async Task SendData(MemoryStream memoryStream)
        {
            await Task.Delay(20);

            var buffer = memoryStream.ToArray();
            var result = await _transmissionExecuter.ReceiveData(buffer, 0, buffer.Length);
            _transmissionProtocol.ReceiveData(result, 0, result.Length);
        }

        [Theory, InlineData(12, 31, 43), InlineData(1, 1, 2), InlineData(234321, 34223, 268544)]
        public async Task TestSumValues(int x, int y, int result)
        {
            Assert.Equal(result, await _transmissionProtocol.Interface.SumValues(x, y));
        }

        [Fact]
        public async Task TestMultipleCallsSameTime()
        {
            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
                tasks.Add(TestSumValues(12, 11, 23));

            await Task.WhenAll(tasks);
        }

        [Theory]
        [InlineData("this is a test", "is", StringComparison.Ordinal, 2)]
        [InlineData("this is a test", "IS", StringComparison.Ordinal, -1)]
        [InlineData("this is a test", "IS", StringComparison.OrdinalIgnoreCase, 2)]
        public async Task TestIndexOf(string value, string needle, StringComparison comparison, int result)
        {
            Assert.Equal(result, await _transmissionProtocol.Interface.IndexOf(value, needle, comparison));
        }

        [Fact]
        public async Task TestCustomObject()
        {
            var result = await _transmissionProtocol.Interface.GetEnvironmentInfo();
            Assert.Equal("asd", result.Test1);
            Assert.Equal(false, result.Test2);
            Assert.Equal(3.141, result.Test3);
        }

        [Fact]
        public async Task TestReturnAbstractClass()
        {
            var result = await _transmissionProtocol.Interface.GetCurrentClient();
            Assert.Equal(123, result.Id);

            var adminClient = Assert.IsType<AdminClient>(result);
            Assert.Equal(adminClient.Permissions, 19);
        }

        [Fact]
        public async Task TestAbstractClassAsParameter()
        {
            var adminClient = new AdminClient {Id = 132};
            var result = await _transmissionProtocol.Interface.GetNextClient(adminClient, "test123");

            Assert.Equal(result.Id, adminClient.Id);

            var userClient = Assert.IsType<UserClient>(result);
            Assert.Equal("test123", userClient.Username);
        }
    }

    public interface ITestInterface
    {
        Task<int> SumValues(int x, int y);
        Task<int> IndexOf(string value, string needle, StringComparison stringComparison);
        Task<EnvironmentInfo> GetEnvironmentInfo();

        [AdditionalTypes(typeof(AdminClient), typeof(UserClient))]
        Task<Client> GetCurrentClient();

        [AdditionalTypes(typeof(AdminClient), typeof(UserClient))]
        Task<Client> GetNextClient([AdditionalTypes(typeof(AdminClient), typeof(UserClient))] Client currentClient, string username);
    }

    public class TestInterfaceImpl : ITestInterface
    {
        public Task<int> SumValues(int x, int y)
        {
            return Task.FromResult(x + y);
        }

        public async Task<int> IndexOf(string value, string needle, StringComparison stringComparison)
        {
            await Task.Delay(50);
            return value.IndexOf(needle, stringComparison);
        }

        public async Task<EnvironmentInfo> GetEnvironmentInfo()
        {
            await Task.Delay(20);
            return new EnvironmentInfo {Test1 = "asd", Test2 = false, Test3 = 3.141};
        }

        public Task<Client> GetCurrentClient()
        {
            return Task.FromResult((Client) new AdminClient{Id = 123, Permissions = 19});
        }

        public Task<Client> GetNextClient(Client currentClient, string username)
        {
            return Task.FromResult((Client) new UserClient {Id = currentClient.Id, Username = username});
        }
    }

    [Serializable]
    public class EnvironmentInfo
    {
        public string Test1 { get; set; }
        public bool Test2 { get; set; }
        public double Test3 { get; set; }
    }

    [Serializable]
    public abstract class Client
    {
        public int Id { get; set; }
    }

    [Serializable]
    public class AdminClient : Client
    {
        public int Permissions { get; set; }
    }

    [Serializable]
    public class UserClient : Client
    {
        public string Username { get; set; }
    }
}