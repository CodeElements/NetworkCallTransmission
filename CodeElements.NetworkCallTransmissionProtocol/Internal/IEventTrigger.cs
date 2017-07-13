using System.Reflection;

namespace CodeElements.NetworkCallTransmissionProtocol.Internal
{
    internal interface IEventTrigger
    {
        void TriggerEvent(EventInfo eventInfo, object parameter);
    }
}