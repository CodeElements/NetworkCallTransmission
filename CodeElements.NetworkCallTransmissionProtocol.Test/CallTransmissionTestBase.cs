using System;
using System.Threading.Tasks;

namespace CodeElements.NetworkCallTransmissionProtocol.Test
{
    public abstract class CallTransmissionTestBase<TInterface>
    {
        protected readonly CallTransmissionExecuter<TInterface> CallTransmissionExecuter;
        protected readonly CallTransmission<TInterface> CallTransmission;

        protected CallTransmissionTestBase(TInterface implementation)
        {
            CallTransmission = new CallTransmission<TInterface>
            {
                SendData = SendData,
                WaitTimeout = TimeSpan.FromSeconds(5)
            };
            CallTransmissionExecuter = new CallTransmissionExecuter<TInterface>(implementation);
        }

        protected virtual async Task SendData(ResponseData data)
        {
            var result = await CallTransmissionExecuter.ReceiveData(data.Data, 0);
            CallTransmission.ReceiveData(result.Data, 0);
        }
    }
}