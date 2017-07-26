using System.Reflection;

namespace CodeElements.NetworkCallTransmission
{
    /// <summary>
    ///     Allows to filter the triggered events of a <see cref="IEventProvider{TEvents}" />
    /// </summary>
    public interface IEventFilter
    {
        /// <summary>
        ///     Called before an event is triggered
        /// </summary>
        /// <param name="eventInfo">The event info</param>
        /// <param name="transmissionInfo">The transmissionInfo of the event</param>
        /// <returns>Return true if the event should be triggered or false if the operation should be canceled</returns>
        bool FilterEvent(EventInfo eventInfo, object transmissionInfo);
    }
}