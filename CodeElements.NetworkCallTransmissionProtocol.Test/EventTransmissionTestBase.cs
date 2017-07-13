using System;
using System.Threading.Tasks;

namespace CodeElements.NetworkCallTransmissionProtocol.Test
{
    public abstract class EventTransmissionTestBase
    {
        protected readonly EventRegister EventRegister;
        protected bool EventSubscriberSentData;

        protected EventTransmissionTestBase()
        {
            EventRegister = new EventRegister();
        }

        protected void ConnectDefaultEventSubscriber(DefaultEventSubscriber defaultEventSubscriber)
        {
            defaultEventSubscriber.SendData += DefaultEventSubscriberOnSendData;
        }

        private void DefaultEventSubscriberOnSendData(object sender, ResponseData responseData)
        {
            EventSubscriberSentData = true;

            var subscriber = (IEventSubscriber) sender;
            EventRegister.ReceiveResponse(responseData.Data, 0, subscriber);
        }
    }

    public class DefaultEventSubscriber : IEventSubscriber
    {
        public DefaultEventSubscriber()
        {
            EventManager = new EventManager{SendData = SendDataHandler};
        }

        public event EventHandler<ResponseData> SendData;

        public bool ReceivedData { get; private set; }
        public EventManager EventManager { get; }

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

        private Task SendDataHandler(ResponseData data)
        {
            SendData?.Invoke(this, data);
            return Task.CompletedTask;
        }
    }
}