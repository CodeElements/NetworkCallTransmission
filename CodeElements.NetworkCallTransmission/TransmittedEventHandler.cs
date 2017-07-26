using System;

namespace CodeElements.NetworkCallTransmission
{
    /// <summary>
    ///     Defines a transmitted event which will be sent to clients
    /// </summary>
    /// <typeparam name="TTransmissionInfo">
    ///     The type of the transmission info. Use <see cref="TransmissionInfo" /> if you don't
    ///     need this argument.
    /// </typeparam>
    /// <param name="transmissionInfo">The transmission info which helps to categorize this event</param>
    [Serializable]
    public delegate void TransmittedEventHandler<in TTransmissionInfo>(TTransmissionInfo transmissionInfo);

    /// <summary>
    ///     Defines a transmitted event which will be sent to clients
    /// </summary>
    /// <typeparam name="TTransmissionInfo">
    ///     The type of the transmission info. Use <see cref="TransmissionInfo" /> if you don't
    ///     need this argument.
    /// </typeparam>
    /// <typeparam name="TEventArgs">The event args which belong to the event</typeparam>
    /// <param name="transmissionInfo">The transmission info which helps to categorize this event</param>
    /// <param name="e">The event args</param>
    [Serializable]
    public delegate void TransmittedEventHandler<in TTransmissionInfo, in TEventArgs>(
        TTransmissionInfo transmissionInfo, TEventArgs e);

    /// <summary>
    ///     Represents a default transmission info
    /// </summary>
    public sealed class TransmissionInfo
    {
        /// <summary>
        ///     The default value (equal to null)
        /// </summary>
        public static TransmissionInfo Empty { get; } = null;
    }
}