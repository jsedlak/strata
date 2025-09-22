using System;
using System.Threading.Tasks;
using Orleans;

namespace Strata.Projections
{
    /// <summary>
    /// Represents a grain that can process projection events.
    /// </summary>
    public interface IProjectionGrain : IGrainWithStringKey
    {
        /// <summary>
        /// Applies a projection event to the specified projection type.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to process.</typeparam>
        /// <param name="event">The event to process.</param>
        /// <param name="projectionType">The type name of the projection to apply the event to.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [OneWay]
        Task ApplyProjection<TEvent>(TEvent @event, string projectionType) 
            where TEvent : class;
    }
}
