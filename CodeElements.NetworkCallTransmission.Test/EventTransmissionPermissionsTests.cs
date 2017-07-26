using System;
using System.Threading.Tasks;
using Xunit;
using ZeroFormatter;

namespace CodeElements.NetworkCallTransmission.Test
{
    public class EventTransmissionPermissionsTests : EventTransmissionTestBase
    {
        private readonly PermissionsTestSubscriber _eventSubscriber;
        private readonly PermissionsTestEventsImpl _permissionsTestEventsImpl;

        public EventTransmissionPermissionsTests()
        {
            EventRegister.RegisterEvents<IPermissionsTestEvents>(_permissionsTestEventsImpl =
                new PermissionsTestEventsImpl());

            _eventSubscriber = new PermissionsTestSubscriber();
            ConnectTestEventSubscriber(_eventSubscriber);
        }

        [Fact]
        public void TestBlockEvent()
        {
            var raised = false;

            void EventsOnTestEvent(object sender, TestObject o)
            {
                Assert.Equal("2A9524F2-8125-4596-8CBE-41F673AF16DB", o.TestString);
                Assert.True(o.TestBool);
                raised = true;
            }

            var events = _eventSubscriber.EventManager.GetEvents<IPermissionsTestEvents>();
            events.Events.TestEvent += EventsOnTestEvent;
            _eventSubscriber.AllowEvent = false;

            _permissionsTestEventsImpl.TriggerEvent(
                new TestObject {TestBool = true, TestString = "2A9524F2-8125-4596-8CBE-41F673AF16DB"});

            Assert.False(raised);

            _eventSubscriber.AllowEvent = true;

            _permissionsTestEventsImpl.TriggerEvent(
                new TestObject { TestBool = true, TestString = "2A9524F2-8125-4596-8CBE-41F673AF16DB" });

            Assert.True(raised);
        }
    }

    public class PermissionsTestSubscriber : IEventSubscriber, ITestEventSubscriber
    {
        public PermissionsTestSubscriber()
        {
            EventManager = new EventManager { SendData = SendDataHandler };
        }

        private Task SendDataHandler(ResponseData data)
        {
            SendData?.Invoke(this, data);
            return Task.CompletedTask;
        }

        public bool AllowEvent { get; set; }
        public EventManager EventManager { get; }

        public Task<bool> CheckPermissions(int[] permissions, object parameter)
        {
            Assert.Equal(1, permissions[0]);
            Assert.Equal(51, permissions[1]);
            Assert.Equal(2, permissions.Length);
            var testObject = Assert.IsType<TestObject>(parameter);

            Assert.Equal("2A9524F2-8125-4596-8CBE-41F673AF16DB", testObject.TestString);
            Assert.True(testObject.TestBool);

            return Task.FromResult(AllowEvent);
        }

        public Task TriggerEvent(byte[] data, int length)
        {
            EventManager.ReceiveData(data, 0);
            return Task.CompletedTask;
        }

        public event EventHandler<ResponseData> SendData;
    }

    public interface IPermissionsTestEvents
    {
        [EventPermissions(1, 51)]
        event TransmittedEventHandler<TransmissionInfo, TestObject> TestEvent;
    }

    public class PermissionsTestEventsImpl : IPermissionsTestEvents
    {
        public event TransmittedEventHandler<TransmissionInfo, TestObject> TestEvent;

        public void TriggerEvent(TestObject args)
        {
            TestEvent?.Invoke(TransmissionInfo.Empty, args);
        }
    }

    [ZeroFormattable]
    public class TestObject
    {
        [Index(0)]
        public virtual string TestString { get; set; }

        [Index(1)]
        public virtual bool TestBool { get; set; }
    }
}