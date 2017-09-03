using System;
using System.Threading.Tasks;

namespace CodeElements.NetworkCallTransmission.Test
{
    public abstract class EventTransmissionTestBase
    {
        protected readonly EventRegister EventRegister;
        protected bool EventSubscriberSentData;

        protected EventTransmissionTestBase()
        {
            EventRegister = new EventRegister();
        }

        protected void ConnectTestEventSubscriber(ITestEventSubscriber testEventSubscriber)
        {
            testEventSubscriber.SendData += DefaultEventSubscriberOnSendData;
        }

        private void DefaultEventSubscriberOnSendData(object sender, ArraySegment<byte> responseData)
        {
            EventSubscriberSentData = true;

            var subscriber = (IEventSubscriber) sender;
            EventRegister.ReceiveResponse(responseData.Array, 0, subscriber);
        }
    }

    public interface ITestEventSubscriber
    {
        event EventHandler<ArraySegment<byte>> SendData;
    }

    public class DefaultEventSubscriber : IEventSubscriber, ITestEventSubscriber
    {
        public DefaultEventSubscriber()
        {
            EventManager = new EventManager{SendData = SendDataHandler};
        }

        public event EventHandler<ArraySegment<byte>> SendData;

        public bool ReceivedData { get; private set; }
        public EventManager EventManager { get; }

        public Task<bool> CheckPermissions(int[] permissions, object parameter)
        {
            return Task.FromResult(true);
        }

        public Task TriggerEvent(byte[] data, int length)
        {
            ReceivedData = true;
            EventManager.ReceiveData(data, 0);
            return Task.CompletedTask;
        }

        public void Reset()
        {
            ReceivedData = false;
        }

        private Task SendDataHandler(ArraySegment<byte> data)
        {
            SendData?.Invoke(this, data);
            return Task.CompletedTask;
        }
    }
}