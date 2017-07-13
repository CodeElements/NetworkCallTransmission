using System;

namespace CodeElements.NetworkCallTransmission
{
    /// <summary>
    ///     Provides methods to subscribe to the remote events
    /// </summary>
    public interface IEventManager
    {
        /// <summary>
        ///     Get events from the remote side
        /// </summary>
        /// <typeparam name="TEventInterface">
        ///     The event contract interface which defines the events. Please note that all events
        ///     must be an <see cref="EventHandler" /> or <see cref="EventHandler{TEventArgs}" />.
        /// </typeparam>
        /// <returns>Return an <see cref="IEventProvider{TEventInterface}" /> which manages the events</returns>
        IEventProvider<TEventInterface> GetEvents<TEventInterface>();

        /// <summary>
        ///     Get events from the remote side from a session
        /// </summary>
        /// <typeparam name="TEventInterface">
        ///     The event contract interface which defines the events. Please note that all events
        ///     must be an <see cref="EventHandler" /> or <see cref="EventHandler" />.
        /// </typeparam>
        /// <param name="eventSessionId">The session id the events were registered with</param>
        /// <returns>Return an <see cref="IEventProvider{TEventInterface}" /> which manages the events</returns>
        IEventProvider<TEventInterface> GetEvents<TEventInterface>(uint eventSessionId);
    }
}