using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CodeElements.NetworkCall.Test
{
    public class CallTransmissionBasicTests : NetworkCallTestBase<IBasicTestInterface>
    {
        public CallTransmissionBasicTests() : base(new BasicTestInterfaceImpl())
        {
        }

        protected override async Task SendData(BufferSegment data, DataTransmitter target)
        {
            await Task.Delay(20);
            await base.SendData(data, target);
        }

        [Theory, InlineData(12, 31, 43), InlineData(1, 1, 2), InlineData(234321, 34223, 268544)]
        public async Task TestSumValues(int x, int y, int result)
        {
            Assert.Equal(result, await Client.Interface.SumValues(x, y));
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
            Assert.Equal(result, await Client.Interface.IndexOf(value, needle, comparison));
        }

        [Fact]
        public async Task TestCustomObject()
        {
            var result = await Client.Interface.GetEnvironmentInfo();
            Assert.Equal("asd", result.Test1);
            Assert.False(result.Test2);
            Assert.Equal(3.141, result.Test3);
        }
    }

    public interface IBasicTestInterface
    {
        Task<int> SumValues(int x, int y);
        Task<int> IndexOf(string value, string needle, StringComparison stringComparison);
        Task<EnvironmentInfo> GetEnvironmentInfo();
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
    }
    
    public class EnvironmentInfo
    {
        public string Test1 { get; set; }
        public bool Test2 { get; set; }
        public double Test3 { get; set; }
    }
}