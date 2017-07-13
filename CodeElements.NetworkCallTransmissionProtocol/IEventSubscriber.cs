using System.Threading.Tasks;

namespace CodeElements.NetworkCallTransmissionProtocol
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
        /// <param name="parameter">The parameter (args) of the event.</param>
        /// <returns>Return a boolean indicating whether the event should be triggered (with <see cref="TriggerEvent" />) or not</returns>
        Task<bool> CheckPermissions(int[] permissions, object parameter);

        /// <summary>
        ///     Trigger an event on the subscriber
        /// </summary>
        /// <param name="data">The event data</param>
        /// <param name="length">The length of the event data buffer</param>
        Task TriggerEvent(byte[] data, int length);
    }
}