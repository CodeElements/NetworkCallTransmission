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
            CallTransmission = new CallTransmission<TInterface>(Properties.Serializer)
            {
                SendData = SendData,
                WaitTimeout = TimeSpan.FromSeconds(5)
            };
            CallTransmissionExecuter = new CallTransmissionExecuter<TInterface>(implementation, Properties.Serializer);
        }

        protected virtual async Task SendData(ArraySegment<byte> data)
        {
            using (var result = await CallTransmissionExecuter.ReceiveData(data.Array, 0))
                CallTransmission.ReceiveData(result.Buffer, 0);
        }
    }
}