using System;
using System.Reflection;

namespace CodeElements.NetworkCallTransmission.EventFilters
{
    /// <summary>
    ///     A simple typed event filter which will only call <see cref="FilterEvent" /> if the transmissionInfo object is of type
    ///     <see cref="TTransmissionInfo" />
    /// </summary>
    /// <typeparam name="TTransmissionInfo">The type of the transmissionInfo when this filter should become active</typeparam>
    public class TypedEventFilter<TTransmissionInfo> : IEventFilter where TTransmissionInfo : class
    {
        /// <summary>
        ///     Called before an event is triggered
        /// </summary>
        /// <param name="transmissionInfo">The transmissionInfo of the event</param>
        /// <returns>Return true if the event should be triggered or false if the operation should be canceled</returns>
        public delegate bool FilerEventDelgate(TTransmissionInfo transmissionInfo);

        /// <summary>
        ///     Initialize a new instance of <see cref="TypedEventFilter{TTransmissionInfo}" />
        /// </summary>
        /// <param name="filerEventDelgate">The filter delegate</param>
        public TypedEventFilter(FilerEventDelgate filerEventDelgate)
        {
            FilterEvent = filerEventDelgate;
        }

        /// <summary>
        ///     Initialize a new instance of <see cref="TypedEventFilter{TTransmissionInfo}" />
        /// </summary>
        public TypedEventFilter()
        {
        }

        /// <summary>
        ///     Set the filter delegate
        /// </summary>
        public FilerEventDelgate FilterEvent { get; set; }

        bool IEventFilter.FilterEvent(EventInfo eventInfo, object transmissionInfo)
        {
            if (FilterEvent == null)
                throw new ArgumentException("The filter event delegate cannot be null.", nameof(FilterEvent));

            var typedTransmissionInfo = transmissionInfo as TTransmissionInfo;
            if (typedTransmissionInfo == null)
                return true;

            return FilterEvent(typedTransmissionInfo);
        }
    }
}