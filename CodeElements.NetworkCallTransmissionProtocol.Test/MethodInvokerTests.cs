using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CodeElements.NetworkCallTransmissionProtocol.Test
{
    public class MethodInvokerTests
    {
        [Fact]
        public async Task TestMethodWithoutParameters()
        {
            var methods = new TestMethods();
            var method =
                methods.GetType().GetMethods().Single(x => x.Name == nameof(TestMethods.MethodWithoutParameters));

            var methodInvoker = new MethodInvoker(method, null, null, 0);
            await methodInvoker.Invoke(methods, null);

            Assert.True(methods.MethodWithoutParametersInvoked);
        }

        [Fact]
        public async Task TestMethodWithParameters()
        {
            var methods = new TestMethods();
            var method =
                methods.GetType().GetMethods().Single(x => x.Name == nameof(TestMethods.MethodWithParameters));

            var methodInvoker = new MethodInvoker(method, null, null, 0);
            await methodInvoker.Invoke(methods, new object[] {"asd", 123, this});

            Assert.True(methods.MethodWithParametersInvoked);
        }

        [Fact]
        public async Task TestMethodWithInvalidTypeParameters()
        {
            var methods = new TestMethods();
            var method =
                methods.GetType().GetMethods().Single(x => x.Name == nameof(TestMethods.MethodWithParameters));

            var methodInvoker = new MethodInvoker(method, null, null, 0);
            await Assert.ThrowsAsync<InvalidCastException>(
                async () => await methodInvoker.Invoke(methods, new object[] {123, "123", true}));
            
            Assert.False(methods.MethodWithParametersInvoked);
        }

        [Fact]
        public async Task TestMethodWithInvalidAmountParameters()
        {
            var methods = new TestMethods();
            var method =
                methods.GetType().GetMethods().Single(x => x.Name == nameof(TestMethods.MethodWithParameters));

            var methodInvoker = new MethodInvoker(method, null, null, 0);
            await Assert.ThrowsAsync<IndexOutOfRangeException>(
                async () => await methodInvoker.Invoke(methods, new object[] {"asd", 123}));

            Assert.False(methods.MethodWithParametersInvoked);
        }

        [Fact]
        public async Task TestMethodWithReturnValue()
        {
            var methods = new TestMethods();
            var method =
                methods.GetType().GetMethods().Single(x => x.Name == nameof(TestMethods.MethodWithReturnValue));

            var methodInvoker = new MethodInvoker(method, null, null, 0);
            var task = methodInvoker.Invoke(methods, null);
            await task;

            Assert.True(methods.MethodWithReturnValueInvoked);

            var result = await Assert.IsType<Task<string>>(task);
            Assert.Equal("test", result);
        }

        private class TestMethods
        {
            public bool MethodWithoutParametersInvoked { get; private set; }
            public bool MethodWithParametersInvoked { get; private set; }
            public bool MethodWithReturnValueInvoked { get; private set; }

            public Task MethodWithoutParameters()
            {
                MethodWithoutParametersInvoked = true;
                return Task.CompletedTask;
            }

            public Task MethodWithParameters(string test1, int test2, MethodInvokerTests customObject)
            {
                MethodWithParametersInvoked = true;
                return Task.CompletedTask;
            }

            public Task<string> MethodWithReturnValue()
            {
                MethodWithReturnValueInvoked = true;
                return Task.FromResult("test");
            }
        }
    }
}