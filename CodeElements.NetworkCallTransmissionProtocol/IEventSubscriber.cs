using System.Threading.Tasks;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    /// <summary>
    ///     Defines a subscriber to specific events
    /// </summary>
    public interface IEventSubscriber
    {
        /// <summary>
        ///     Trigger an event on the subscriber
        /// </summary>
        /// <param name="data">The event data</param>
        /// <param name="length">The length of the event data buffer</param>
        Task TriggerEvent(byte[] data, int length);
    }
}