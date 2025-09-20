using System;
using Orleans;

namespace Strata.Tests.EventHandlers;

/// <summary>
/// Basic test event for event handler testing.
/// </summary>
[GenerateSerializer]
public class TestEvent
{
    [Id(0)]
    public string Message { get; set; } = string.Empty;
    [Id(1)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Typed test event for typed handler testing.
/// </summary>
[GenerateSerializer]
public class TypedTestEvent
{
    [Id(0)]
    public int Value { get; set; }
    [Id(1)]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Event that causes handler errors for error handling testing.
/// </summary>
[GenerateSerializer]
public class ErrorTestEvent
{
    [Id(0)]
    public string ErrorMessage { get; set; } = "Test error";
    [Id(1)]
    public bool ShouldThrow { get; set; } = true;
}

/// <summary>
/// Event for async handler testing.
/// </summary>
[GenerateSerializer]
public class AsyncTestEvent
{
    [Id(0)]
    public int DelayMs { get; set; } = 100;
    [Id(1)]
    public string Result { get; set; } = string.Empty;
}

/// <summary>
/// Event for performance testing.
/// </summary>
[GenerateSerializer]
public class PerformanceTestEvent
{
    [Id(0)]
    public int Id { get; set; }
    [Id(1)]
    public string Data { get; set; } = string.Empty;
}

/// <summary>
/// Event for testing handler execution order.
/// </summary>
[GenerateSerializer]
public class OrderTestEvent
{
    [Id(0)]
    public int Sequence { get; set; }
    [Id(1)]
    public string HandlerName { get; set; } = string.Empty;
}
