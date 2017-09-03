using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using ZeroFormatter;

namespace CodeElements.NetworkCallTransmission.Test
{
    public class CallTransmissionBasicTests : CallTransmissionTestBase<IBasicTestInterface>
    {
        public CallTransmissionBasicTests() : base(new BasicTestInterfaceImpl())
        {
        }

        protected override async Task SendData(ArraySegment<byte> data)
        {
            await Task.Delay(20);
            await base.SendData(data);
        }

        [Theory, InlineData(12, 31, 43), InlineData(1, 1, 2), InlineData(234321, 34223, 268544)]
        public async Task TestSumValues(int x, int y, int result)
        {
            Assert.Equal(result, await CallTransmission.Interface.SumValues(x, y));
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
            Assert.Equal(result, await CallTransmission.Interface.IndexOf(value, needle, comparison));
        }

        [Fact]
        public async Task TestCustomObject()
        {
            var result = await CallTransmission.Interface.GetEnvironmentInfo();
            Assert.Equal("asd", result.Test1);
            Assert.False(result.Test2);
            Assert.Equal(3.141, result.Test3);
        }

        [Fact]
        public async Task TestReturnAbstractClass()
        {
            var result = await CallTransmission.Interface.GetCurrentClient();
            Assert.Equal(123, result.Id);

            var adminClient = Assert.IsAssignableFrom<AdminClient>(result);
            Assert.Equal(19, adminClient.Permissions);
        }

        [Fact]
        public async Task TestAbstractClassAsParameter()
        {
            var adminClient = new AdminClient {Id = 132};
            var result = await CallTransmission.Interface.GetNextClient(adminClient, "test123");

            Assert.Equal(result.Id, adminClient.Id);

            var userClient = Assert.IsAssignableFrom<UserClient>(result);
            Assert.Equal("test123", userClient.Username);
        }
    }

    public interface IBasicTestInterface
    {
        Task<int> SumValues(int x, int y);
        Task<int> IndexOf(string value, string needle, StringComparison stringComparison);
        Task<EnvironmentInfo> GetEnvironmentInfo();

        Task<Client> GetCurrentClient();
        Task<Client> GetNextClient(Client currentClient, string username);
    }

    public class BasicTestInterfaceImpl : IBasicTestInterface
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

    [ZeroFormattable]
    public class EnvironmentInfo
    {
        [Index(0)]
        public virtual string Test1 { get; set; }

        [Index(1)]
        public virtual bool Test2 { get; set; }

        [Index(2)]
        public virtual double Test3 { get; set; }
    }

    [ZeroFormattable, Union(typeof(AdminClient), typeof(UserClient))]
    public abstract class Client
    {
        [UnionKey]
        public abstract byte ClientType { get; }

        [Index(0)]
        public virtual int Id { get; set; }
    }

    [ZeroFormattable]
    public class AdminClient : Client
    {
        [Index(1)]
        public virtual int Permissions { get; set; }

        public override byte ClientType { get; } = 1;
    }

    [ZeroFormattable]
    public class UserClient : Client
    {
        [Index(1)]
        public virtual string Username { get; set; }

        public override byte ClientType { get; } = 2;
    }
}