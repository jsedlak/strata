using System;

namespace Strata.Eventing;

/// <summary>
/// Delegate for handling typed events in an event sourced grain.
/// </summary>
/// <typeparam name="TEvent">The type of event to handle.</typeparam>
/// <param name="event">The event instance to handle.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task EventHandlerDelegate<in TEvent>(TEvent @event);

/// <summary>
/// Delegate for handling untyped events in an event sourced grain.
/// </summary>
/// <param name="event">The event instance to handle as an object.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task EventHandlerDelegate(object @event);
