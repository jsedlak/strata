using System;

namespace Strata.Eventing;

/// <summary>
/// Configuration options for event handler behavior.
/// </summary>
public class EventHandlerOptions
{
    /// <summary>
    /// Gets or sets whether to fail fast when a handler throws an exception.
    /// When false (default), handler failures are logged but processing continues.
    /// </summary>
    public bool FailFastOnHandlerError { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum execution time for a single handler.
    /// When null (default), no timeout is applied.
    /// </summary>
    public TimeSpan? MaxHandlerExecutionTime { get; set; } = null;

    /// <summary>
    /// Gets or sets whether to log handler execution details.
    /// </summary>
    public bool LogHandlerExecution { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to log handler errors.
    /// </summary>
    public bool LogHandlerErrors { get; set; } = true;
}
