using System.Reflection;

namespace CodeElements.NetworkCall.Internal
{
    internal interface IEventTrigger
    {
        void TriggerEvent(EventInfo eventInfo, object parameter);
    }
}