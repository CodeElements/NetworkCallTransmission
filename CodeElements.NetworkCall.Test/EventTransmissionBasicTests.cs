using System;
using System.Threading.Tasks;
using Xunit;

namespace CodeElements.NetworkCall.Test
{
    public class EventTransmissionBasicTests : NetworkCallTestBase<IBasicTestEvents>
    {
        private bool _dataSent;
        private int _sendCounter;

        public EventTransmissionBasicTests() : base(new BasicTestEventsImpl())
        {
        }

        public BasicTestEventsImpl BasicTestEventsImpl => (BasicTestEventsImpl) Implementation;

        protected override Task SendData(BufferSegment data, DataTransmitter target)
        {
            _dataSent = true;
            _sendCounter++;
            return base.SendData(data, target);
        }

        [Fact]
        public void TestEventManagerReceiveEvent()
        {
            var receivedEvent1 = false;

            void Handler1(object sender, EventArgs args)
            {
                receivedEvent1 = true;
            }

            Implementation.TestEvent1 += Handler1;

            Assert.False(receivedEvent1);

            BasicTestEventsImpl.TriggerTestEvent1();
            Assert.True(receivedEvent1);
            
            receivedEvent1 = false;
            Implementation.TestEvent1 -= Handler1;

            BasicTestEventsImpl.TriggerTestEvent1();
            Assert.False(receivedEvent1);
        }

        [Fact]
        public void TestSubscribeMultipleTimes()
        {
            void Handler(object sender, EventArgs args) { }
            void Handler2(object sender, EventArgs args) { }

            Assert.Equal(0, _sendCounter);
            Client.Interface.TestEvent1 += Handler;

            Assert.Equal(1, _sendCounter);
            Client.Interface.TestEvent1 += Handler;
            Client.Interface.TestEvent1 += Handler;
            Client.Interface.TestEvent1 += Handler;

            Assert.Equal(1, _sendCounter);

            Client.Interface.TestEvent1 -= Handler;
            Client.Interface.TestEvent1 -= Handler;
            Client.Interface.TestEvent1 -= Handler;
            Client.Interface.TestEvent1 -= Handler2;
            Client.Interface.TestEvent1 -= Handler2;
            Client.Interface.TestEvent1 -= Handler2;

            Assert.Equal(1, _sendCounter);
            Client.Interface.TestEvent1 -= Handler;
            Assert.Equal(2, _sendCounter);
            Client.Interface.TestEvent1 -= Handler2;
            Client.Interface.TestEvent1 -= Handler;
            Assert.Equal(2, _sendCounter);
        }
        
        [Fact]
        public void TestEventManagerSubscribeUnsubscribeEvents()
        {
            void Nop()
            {
            }

            void Handler1(object sender, EventArgs args)
            {
                Nop();
            }

            void Handler2(object sender, string args)
            {
                Nop();
            }

            void Handler3(object sender, bool args)
            {
                Nop();
            }

            void Handler31(object sender, bool args)
            {
                Nop();
            }

            void Handler32(object sender, bool args)
            {
                Nop();
            }

            Assert.Equal(0, _sendCounter);
            Client.Interface.TestEvent1 += Handler1;

            Assert.Equal(1, _sendCounter);
            Client.Interface.TestEvent2 += Handler2;

            Assert.Equal(2, _sendCounter);
            Client.Interface.TestEvent1 -= Handler1;

            Assert.Equal(3, _sendCounter);
            Client.Interface.TestEvent3 += Handler3;

            Assert.Equal(4, _sendCounter);
            Client.Interface.TestEvent3 += Handler31;

            Assert.Equal(4, _sendCounter);
            Client.Interface.TestEvent3 -= Handler3;

            Assert.Equal(4, _sendCounter);
            Client.Interface.TestEvent3 -= Handler32;

            Assert.Equal(4, _sendCounter);
            Client.Interface.TestEvent3 += Handler32;

            Assert.Equal(4, _sendCounter);
            Client.Interface.TestEvent2 -= Handler2;
            Assert.Equal(5, _sendCounter);
        }

        [Fact]
        public void TestEventManagerSuspendResume()
        {
            var receivedEvent3 = false;

            void Handler3(object sender, bool args)
            {
                receivedEvent3 = true;
            }
            
            Client.SuspendSubscribing();
            Client.Interface.TestEvent3 += Handler3;

            Assert.False(_dataSent);
            BasicTestEventsImpl.TriggerTestEvent3(false);
            Assert.False(receivedEvent3);

            Client.ResumeSubscribing();
            Assert.True(_dataSent);

            BasicTestEventsImpl.TriggerTestEvent3(true);
            Assert.True(receivedEvent3);
        }

        [Fact]
        public void TestEventRegisterTriggerWithoutClients()
        {
            BasicTestEventsImpl.TriggerTestEvent1();
            BasicTestEventsImpl.TriggerTestEvent2("as435");
        }
    }

    public interface IBasicTestEvents
    {
        event EventHandler TestEvent1;
        event EventHandler<string> TestEvent2;
        event EventHandler<bool> TestEvent3;

        Task Invoke();
    }

    public class BasicTestEventsImpl : IBasicTestEvents
    {
        public void TriggerTestEvent1()
        {
            TestEvent1?.Invoke(this, EventArgs.Empty);
        }

        public void TriggerTestEvent2(string data)
        {
            TestEvent2?.Invoke(this, data);
        }

        public void TriggerTestEvent3(bool data)
        {
            TestEvent3?.Invoke(this, data);
        }

        public event EventHandler TestEvent1;
        public event EventHandler<string> TestEvent2;
        public event EventHandler<bool> TestEvent3;
        public Task Invoke() => throw new NotImplementedException();
    }
}