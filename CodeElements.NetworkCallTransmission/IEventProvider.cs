using System;

namespace CodeElements.NetworkCallTransmission
{
    /// <summary>
    ///     Manages events of a contract
    /// </summary>
    /// <typeparam name="TEvents">The contract interface</typeparam>
    public interface IEventProvider<out TEvents> : IDisposable
    {
        /// <summary>
        ///     The events
        /// </summary>
        TEvents Events { get; }

        /// <summary>
        ///     Suspend the transmission of event subscriptions, just keep them in memory until <see cref="ResumeSubscribing" /> is
        ///     called. If mulitple events must be registered, this method compresses the data which must be sent to a single
        ///     package (which will be sent when <see cref="ResumeSubscribing" /> is called)
        /// </summary>
        void SuspendSubscribing();

        /// <summary>
        ///     Resume the transmision of event subscriptions and transmit the subscription of all events which were subscribed
        ///     after <see cref="SuspendSubscribing" /> was called. All events subscribed from now on will be instantly
        ///     transmitted.
        /// </summary>
        void ResumeSubscribing();

        /// <summary>
        ///     Add a new event filter
        /// </summary>
        /// <param name="eventFilter">The filter which determines which events should be triggered</param>
        void AddFilter(IEventFilter eventFilter);
    }
}