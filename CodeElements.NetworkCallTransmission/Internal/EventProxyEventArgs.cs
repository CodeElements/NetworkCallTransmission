using System;

namespace CodeElements.NetworkCallTransmission.Internal
{
    internal class EventProxyEventArgs : EventArgs
    {
        public EventProxyEventArgs(ulong eventId, object transmissionInfo, object eventArgs)
        {
            EventId = eventId;
            TransmissionInfo = transmissionInfo;
            EventArgs = eventArgs;
        }

        public ulong EventId { get; }
        public object TransmissionInfo { get; }
        public object EventArgs { get; }
    }
}