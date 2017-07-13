using System;
using System.Diagnostics;
using Xunit;

namespace CodeElements.NetworkCallTransmission.Test
{
    public class EventTransmissionBasicTests : EventTransmissionTestBase
    {
        private readonly DefaultEventSubscriber _eventSubscriber;
        private readonly BasicTestEventsImpl _basicTestEventsImpl;

        public EventTransmissionBasicTests()
        {
            EventRegister.RegisterEvents<IBasicTestEvents>(_basicTestEventsImpl = new BasicTestEventsImpl());

            _eventSubscriber = new DefaultEventSubscriber();
            ConnectTestEventSubscriber(_eventSubscriber);
        }

        [Fact]
        public void TestEventManagerGetEvents()
        {
            var events = _eventSubscriber.EventManager.GetEvents<IBasicTestEvents>();
            Assert.NotNull(events);
            Assert.NotNull(events.Events);
        }

        [Fact]
        public void TestEventManagerSubscribeUnsubscribeEvents()
        {
            void Handler1(object sender, EventArgs args) => Debug.Print("");
            void Handler2(object sender, string args) => Debug.Print("");
            void Handler3(object sender, bool args) => Debug.Print("");
            void Handler31(object sender, bool args) => Debug.Print("");
            void Handler32(object sender, bool args) => Debug.Print("");

            var events = _eventSubscriber.EventManager.GetEvents<IBasicTestEvents>();
            events.Events.TestEvent1 += Handler1;
            events.Events.TestEvent2 += Handler2;
            events.Events.TestEvent1 -= Handler1;
            events.Events.TestEvent3 += Handler3;
            events.Events.TestEvent3 += Handler31;
            events.Events.TestEvent3 -= Handler3;
            events.Events.TestEvent3 -= Handler32;
            events.Events.TestEvent3 += Handler32;
            events.Events.TestEvent2 -= Handler2;
        }

        [Fact]
        public void TestEventRegisterTriggerWithoutClients()
        {
            _basicTestEventsImpl.TriggerTestEvent1();
            _basicTestEventsImpl.TriggerTestEvent2("asd");
        }

        [Fact]
        public void TestEventManagerReceiveEvent()
        {
            var receivedEvent1 = false;
            void Handler1(object sender, EventArgs args) => receivedEvent1 = true;

            var events = _eventSubscriber.EventManager.GetEvents<IBasicTestEvents>();
            events.Events.TestEvent1 += Handler1;
            Assert.False(receivedEvent1);
            Assert.False(_eventSubscriber.ReceivedData);

            _basicTestEventsImpl.TriggerTestEvent1();
            Assert.True(receivedEvent1);
            Assert.True(_eventSubscriber.ReceivedData);

            _eventSubscriber.Reset();
            receivedEvent1 = false;

            events.Events.TestEvent1 -= Handler1;

            _basicTestEventsImpl.TriggerTestEvent1();
            Assert.False(_eventSubscriber.ReceivedData);
            Assert.False(receivedEvent1);
        }

        [Fact]
        public void TestEventManagerSuspendResume()
        {
            var receivedEvent3 = false;
            void Handler3(object sender, bool args) => receivedEvent3 = receivedEvent3 = true;

            var events = _eventSubscriber.EventManager.GetEvents<IBasicTestEvents>();
            events.SuspendSubscribing();
            events.Events.TestEvent3 += Handler3;

            Assert.False(EventSubscriberSentData);
            _basicTestEventsImpl.TriggerTestEvent3(false);
            Assert.False(receivedEvent3);

            events.ResumeSubscribing();
            Assert.True(EventSubscriberSentData);

            _basicTestEventsImpl.TriggerTestEvent3(true);
            Assert.True(receivedEvent3);
        }
    }

    public interface IBasicTestEvents
    {
        event EventHandler TestEvent1;
        event EventHandler<string> TestEvent2;
        event EventHandler<bool> TestEvent3;
    }

    public class BasicTestEventsImpl : IBasicTestEvents
    {
        public event EventHandler TestEvent1;
        public event EventHandler<string> TestEvent2;
        public event EventHandler<bool> TestEvent3;

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
    }
}
