using System;
using System.Threading.Tasks;

namespace CodeElements.NetworkCallTransmissionProtocol.Test
{
    public abstract class CallTransmissionTestBase<TInterface>
    {
        protected readonly CallTransmissionExecuter<TInterface> CallTransmissionExecuter;
        protected readonly CallTransmissionProtocol<TInterface> CallTransmissionProtocol;

        protected CallTransmissionTestBase(TInterface implementation)
        {
            CallTransmissionProtocol = new CallTransmissionProtocol<TInterface>
            {
                SendData = SendData,
                WaitTimeout = TimeSpan.FromSeconds(5)
            };
            CallTransmissionExecuter = new CallTransmissionExecuter<TInterface>(implementation);
        }

        protected virtual async Task SendData(ResponseData data)
        {
            var result = await CallTransmissionExecuter.ReceiveData(data.Data, 0, data.Length);
            CallTransmissionProtocol.ReceiveData(result.Data, 0, result.Length);
        }
    }
}