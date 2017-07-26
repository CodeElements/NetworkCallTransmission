using System.Reflection;

namespace CodeElements.NetworkCallTransmission.Internal
{
    internal interface IEventTrigger
    {
        void TriggerEvent(EventInfo eventInfo, object transmissionInfo, object parameter);
    }
}