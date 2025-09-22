using System;
using System.Threading.Tasks;

namespace Strata.Projections
{
    /// <summary>
    /// Represents a projection that can handle events of a specific type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event this projection can handle.</typeparam>
    public interface IProjection<in TEvent>
    {
        /// <summary>
        /// Handles the specified event.
        /// </summary>
        /// <param name="event">The event to handle.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task Handle(TEvent @event);
    }
}
