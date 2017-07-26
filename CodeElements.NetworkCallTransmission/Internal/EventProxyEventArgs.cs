using System;

namespace CodeElements.NetworkCallTransmission.Internal
{
    internal class EventProxyEventArgs : EventArgs
    {
        public EventProxyEventArgs(ulong eventId, object transmissionInfo, object parameter)
        {
            EventId = eventId;
            TransmissionInfo = transmissionInfo;
            Parameter = parameter;
        }

        public ulong EventId { get; }
        public object TransmissionInfo { get; }
        public object Parameter { get; }
    }
}