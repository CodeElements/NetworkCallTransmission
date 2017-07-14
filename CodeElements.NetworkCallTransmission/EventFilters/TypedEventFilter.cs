using System;
using System.Reflection;

namespace CodeElements.NetworkCallTransmission.EventFilters
{
    /// <summary>
    ///     A simple typed event filter which will only call <see cref="FilterEvent" /> if the parameter object is of type
    ///     <see cref="TParameter" />
    /// </summary>
    /// <typeparam name="TParameter">The type of the parameter when this filter should become active</typeparam>
    public class TypedEventFilter<TParameter> : IEventFilter where TParameter : class
    {
        /// <summary>
        ///     Called before an event is triggered
        /// </summary>
        /// <param name="parameter">The parameter of the event</param>
        /// <returns>Return true if the event should be triggered or false if the operation should be canceled</returns>
        public delegate bool FilerEventDelgate(TParameter parameter);

        /// <summary>
        ///     Initialize a new instance of <see cref="TypedEventFilter{TParameter}" />
        /// </summary>
        /// <param name="filerEventDelgate">The filter delegate</param>
        public TypedEventFilter(FilerEventDelgate filerEventDelgate)
        {
            FilterEvent = filerEventDelgate;
        }

        /// <summary>
        ///     Initialize a new instance of <see cref="TypedEventFilter{TParameter}" />
        /// </summary>
        public TypedEventFilter()
        {
        }

        /// <summary>
        ///     Set the filter delegate
        /// </summary>
        public FilerEventDelgate FilterEvent { get; set; }

        bool IEventFilter.FilterEvent(EventInfo eventInfo, object parameter)
        {
            if (FilterEvent == null)
                throw new ArgumentException("The filter event delegate cannot be null.", nameof(FilterEvent));

            var typedParameter = parameter as TParameter;
            if (typedParameter == null)
                return true;

            return FilterEvent(typedParameter);
        }
    }
}