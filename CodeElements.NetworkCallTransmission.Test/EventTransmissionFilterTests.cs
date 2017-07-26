using CodeElements.NetworkCallTransmission.EventFilters;
using Xunit;

namespace CodeElements.NetworkCallTransmission.Test
{
    public class EventTransmissionFilterTests : EventTransmissionTestBase
    {
        private readonly DefaultEventSubscriber _eventSubscriber;
        private readonly BasicTestEventsImpl _basicTestEventsImpl;

        public EventTransmissionFilterTests()
        {
            EventRegister.RegisterEvents<IBasicTestEvents>(_basicTestEventsImpl = new BasicTestEventsImpl());

            _eventSubscriber = new DefaultEventSubscriber();
            ConnectTestEventSubscriber(_eventSubscriber);
        }

        [Fact]
        public void TestFilter()
        {
            var events = _eventSubscriber.EventManager.GetEvents<IBasicTestEvents>();
            events.AddFilter(new TypedEventFilter<string>(x => x != "test"));

            var raised1 = false;
            var raised2 = false;

            events.Events.TestEvent1 += info => raised1 = true;

            _basicTestEventsImpl.TriggerTestEvent1();
            Assert.True(raised1);

            events.Events.TestEvent2 += (transInfo, args) => raised2 = true;
            _basicTestEventsImpl.TriggerTestEvent2("324asd", "test");
            Assert.False(raised2);

            _basicTestEventsImpl.TriggerTestEvent2("asd", "a3");
            Assert.True(raised2);
        }
    }
}