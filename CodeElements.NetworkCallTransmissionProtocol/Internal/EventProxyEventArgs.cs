using System;

namespace CodeElements.NetworkCallTransmissionProtocol.Internal
{
    internal class EventProxyEventArgs : EventArgs
    {
        public EventProxyEventArgs(ulong eventId, object parameter)
        {
            EventId = eventId;
            Parameter = parameter;
        }

        public ulong EventId { get; }
        public object Parameter { get; }
    }
}