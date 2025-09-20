using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Strata.Eventing;

/// <summary>
/// Registry for managing event handler registrations in an event sourced grain.
/// </summary>
public class EventHandlerRegistry
{
    private readonly ConcurrentBag<HandlerRegistration> _typedHandlers = new();
    private readonly ConcurrentBag<EventHandlerDelegate> _untypedHandlers = new();
    private int _registrationOrder = 0;

    /// <summary>
    /// Registers a typed event handler for a specific event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to handle.</typeparam>
    /// <param name="handler">The handler delegate.</param>
    public void RegisterEventHandler<TEvent>(EventHandlerDelegate<TEvent> handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var registration = new HandlerRegistration
        {
            Handler = (eventObj) => handler((TEvent)eventObj),
            EventType = typeof(TEvent),
            RegistrationOrder = Interlocked.Increment(ref _registrationOrder)
        };

        _typedHandlers.Add(registration);
    }

    /// <summary>
    /// Registers an untyped event handler that will be called for all events.
    /// </summary>
    /// <param name="handler">The handler delegate.</param>
    public void RegisterEventHandler(EventHandlerDelegate handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        _untypedHandlers.Add(handler);
    }

    /// <summary>
    /// Gets all handlers that should be called for a specific event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event.</typeparam>
    /// <returns>An enumerable of handler delegates in registration order.</returns>
    public IEnumerable<EventHandlerDelegate> GetHandlersForEvent<TEvent>()
    {
        return GetHandlersForEvent(typeof(TEvent));
    }

    /// <summary>
    /// Gets all handlers that should be called for a specific event type.
    /// </summary>
    /// <param name="eventType">The type of event.</param>
    /// <returns>An enumerable of handler delegates in registration order.</returns>
    public IEnumerable<EventHandlerDelegate> GetHandlersForEvent(Type eventType)
    {
        var typedHandlers = _typedHandlers
            .Where(h => h.EventType == eventType)
            .OrderBy(h => h.RegistrationOrder)
            .Select(h => h.Handler);

        var untypedHandlers = _untypedHandlers;

        return typedHandlers.Concat(untypedHandlers);
    }

    /// <summary>
    /// Gets all untyped handlers in registration order.
    /// </summary>
    /// <returns>An enumerable of untyped handler delegates.</returns>
    public IEnumerable<EventHandlerDelegate> GetAllHandlers()
    {
        return _untypedHandlers;
    }

    /// <summary>
    /// Clears all registered handlers.
    /// </summary>
    public void Clear()
    {
        while (_typedHandlers.TryTake(out _)) { }
        while (_untypedHandlers.TryTake(out _)) { }
        _registrationOrder = 0;
    }

    /// <summary>
    /// Gets the total number of registered handlers.
    /// </summary>
    public int HandlerCount => _typedHandlers.Count + _untypedHandlers.Count;

    private class HandlerRegistration
    {
        public EventHandlerDelegate Handler { get; set; } = null!;
        public Type EventType { get; set; } = null!;
        public int RegistrationOrder { get; set; }
    }
}
