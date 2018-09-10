using System;
using System.Buffers;
using System.Threading.Tasks;

namespace CodeElements.NetworkCall.Test
{
    public abstract class NetworkCallTestBase<TInterface>
    {
        protected readonly NetworkCallClient<TInterface> Client;
        protected readonly NetworkCallServer<TInterface> Server;

        protected NetworkCallTestBase(TInterface implementation)
        {
            Implementation = implementation;
            Client = new NetworkCallClient<TInterface>(Properties.Serializer, ArrayPool<byte>.Shared)
            {
                WaitTimeout = TimeSpan.FromSeconds(5),
                SendData = data => SendData(data, Server)
            };

            Server = new NetworkCallServer<TInterface>(implementation, Properties.Serializer)
            {
                SendData = data => SendData(data, Client)
            };
        }

        protected TInterface Implementation { get; }

        protected virtual Task SendData(BufferSegment data, DataTransmitter target)
        {
            target.ReceiveData(data.Buffer, data.Offset);
            return Task.CompletedTask;
        }
    }
}