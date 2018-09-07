using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodeElements.NetworkCall.NetSerializer;
using Xunit;

namespace CodeElements.NetworkCall.Test
{
    public class SampleTest : NetworkCallTestBase<ITestInterface>
    {
        private readonly List<ProgressChangedArgs> _events;

        public SampleTest() : base(new ProgressTest())
        {
            _events = new List<ProgressChangedArgs>();
        }

        private void OnProgressChanged(object sender, ProgressChangedArgs e)
        {
            _events.Add(e);
        }

        [Fact]
        public async Task Test()
        {
            Client.Interface.ProgressChanged += OnProgressChanged;
            var result = await Client.Interface.InvokeAction();
            Assert.Equal("Hello World", result);

            Assert.Equal(10, _events.Count);
            for (var i = 0; i < _events.Count; i++)
            {
                var progressChangedArg = _events[i];
                Assert.Equal(i / 10d, progressChangedArg.Progress);
            }
        }
    }

    public class ProgressTest : ITestInterface
    {
        public event EventHandler<ProgressChangedArgs> ProgressChanged;

        public async Task<string> InvokeAction()
        {
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(5);
                ProgressChanged?.Invoke(this, new ProgressChangedArgs {Progress = i / 10d});
            }

            return "Hello World";
        }
    }

    public interface ITestInterface
    {
        event EventHandler<ProgressChangedArgs> ProgressChanged;

        Task<string> InvokeAction();
    }

    public class ProgressChangedArgs
    {
        public ProgressChangedArgs(double progress)
        {
            Progress = progress;
        }

        public ProgressChangedArgs()
        {
        }

        public double Progress { get; set; }
    }
}
