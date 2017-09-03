using System;
using System.Threading.Tasks;

namespace CodeElements.NetworkCallTransmission.Test
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

        protected virtual async Task SendData(ArraySegment<byte> data)
        {
            var result = await CallTransmissionExecuter.ReceiveData(data.Array, 0);
            CallTransmission.ReceiveData(result.Array, 0);
        }
    }
}