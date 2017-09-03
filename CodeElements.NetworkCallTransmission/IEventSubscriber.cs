using System.Threading.Tasks;

namespace CodeElements.NetworkCallTransmission
{
    /// <summary>
    ///     Defines a subscriber to specific events
    /// </summary>
    public interface IEventSubscriber
    {
        /// <summary>
        ///     Check the permissions for an <see cref="IEventSubscriber" /> to receive an event
        /// </summary>
        /// <param name="permissions">
        ///     The permissions which were specified in the <see cref="EventPermissionsAttribute" /> of the
        ///     event. Null if the event doesn't has that attribute.
        /// </param>
        /// <param name="transmissionInfo">The transmission info of the event.</param>
        /// <returns>Return a boolean indicating whether the event should be triggered (with <see cref="TriggerEvent" />) or not</returns>
        Task<bool> CheckPermissions(int[] permissions, object transmissionInfo);

        /// <summary>
        ///     Trigger an event on the subscriber
        /// </summary>
        /// <param name="buffer">The event buffer</param>
        /// <param name="offset">The offset of the data in the buffer</param>
        /// <param name="length">The length of the event buffer</param>
        Task TriggerEvent(byte[] buffer, int offset, int length);
    }
}