using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Strata.Tests.EventHandlers;

namespace Strata.Tests.EventHandlers;

/// <summary>
/// Test state for event handler testing.
/// </summary>
[GenerateSerializer]
public class TestState
{
    [Id(0)]
    public List<string> HandlerCalls { get; set; } = new();
    [Id(1)]
    public int EventCount { get; set; }
    [Id(2)]
    public DateTime LastEventTime { get; set; }
    [Id(3)]
    public string LastEventType { get; set; } = string.Empty;
}

/// <summary>
/// Test grain that uses event handlers for testing.
/// </summary>
public class TestEventHandlerGrain : EventSourcedGrain<TestState, object>, ITestEventHandlerGrain
{
    public bool HandlersRegistered { get; private set; } = false;
    public int HandlerCount { get; private set; } = 0;

    protected override void OnSetupEventHandlers()
    {
        HandlersRegistered = true;
        
        // Register some test handlers during setup
        RegisterEventHandler<TestEvent>(HandleTestEvent);
        RegisterEventHandler<TypedTestEvent>(HandleTypedTestEvent);
        RegisterEventHandler<ErrorTestEvent>(HandleErrorTestEvent);
        RegisterEventHandler<AsyncTestEvent>(HandleAsyncTestEvent);
        RegisterEventHandler<OrderTestEvent>(HandleOrderTestEvent);
        
        // Register untyped handler
        RegisterEventHandler(HandleAllEvents);
        
        HandlerCount = 6; // We registered 6 handlers
    }

    private Task HandleTestEvent(TestEvent @event)
    {
        TentativeState.HandlerCalls.Add($"TestEvent: {@event.Message}");
        TentativeState.EventCount++;
        TentativeState.LastEventTime = @event.Timestamp;
        TentativeState.LastEventType = nameof(TestEvent);
        return Task.CompletedTask;
    }

    private Task HandleTypedTestEvent(TypedTestEvent @event)
    {
        TentativeState.HandlerCalls.Add($"TypedTestEvent: {@event.Value} - {@event.Description}");
        TentativeState.EventCount++;
        TentativeState.LastEventType = nameof(TypedTestEvent);
        return Task.CompletedTask;
    }

    private Task HandleErrorTestEvent(ErrorTestEvent @event)
    {
        if (@event.ShouldThrow)
        {
            throw new InvalidOperationException(@event.ErrorMessage);
        }
        
        TentativeState.HandlerCalls.Add($"ErrorTestEvent: {@event.ErrorMessage}");
        TentativeState.EventCount++;
        TentativeState.LastEventType = nameof(ErrorTestEvent);
        return Task.CompletedTask;
    }

    private async Task HandleAsyncTestEvent(AsyncTestEvent @event)
    {
        await Task.Delay(@event.DelayMs);
        TentativeState.HandlerCalls.Add($"AsyncTestEvent: {@event.Result}");
        TentativeState.EventCount++;
        TentativeState.LastEventType = nameof(AsyncTestEvent);
    }

    private Task HandleOrderTestEvent(OrderTestEvent @event)
    {
        TentativeState.HandlerCalls.Add($"OrderTestEvent: {@event.Sequence} - {@event.HandlerName}");
        TentativeState.EventCount++;
        TentativeState.LastEventType = nameof(OrderTestEvent);
        return Task.CompletedTask;
    }

    private Task HandleAllEvents(object @event)
    {
        TentativeState.HandlerCalls.Add($"Untyped: {@event.GetType().Name}");
        return Task.CompletedTask;
    }

    public Task RaiseTestEvent(TestEvent @event)
    {
        Raise(@event);
        return Task.CompletedTask;
    }

    public Task RaiseTypedTestEvent(TypedTestEvent @event)
    {
        Raise(@event);
        return Task.CompletedTask;
    }

    public Task RaiseErrorTestEvent(ErrorTestEvent @event)
    {
        Raise(@event);
        return Task.CompletedTask;
    }

    public Task RaiseAsyncTestEvent(AsyncTestEvent @event)
    {
        Raise(@event);
        return Task.CompletedTask;
    }

    public Task RaiseOrderTestEvent(OrderTestEvent @event)
    {
        Raise(@event);
        return Task.CompletedTask;
    }

    public Task RaiseMultipleEvents(IEnumerable<object> events)
    {
        Raise(events);
        return Task.CompletedTask;
    }

    public Task<TestState> GetState()
    {
        return Task.FromResult(TentativeState);
    }

    public Task<List<string>> GetHandlerCalls()
    {
        return Task.FromResult(new List<string>(TentativeState.HandlerCalls));
    }

    public Task<int> GetEventCount()
    {
        return Task.FromResult(TentativeState.EventCount);
    }

    public Task<bool> GetHandlersRegistered()
    {
        return Task.FromResult(HandlersRegistered);
    }

    public Task<int> GetRegisteredHandlerCount()
    {
        return Task.FromResult(HandlerCount);
    }
}

/// <summary>
/// Test grain for performance testing with many handlers.
/// </summary>
public class PerformanceTestGrain : EventSourcedGrain<TestState, object>, IPerformanceTestGrain
{
    public bool HandlersRegistered { get; private set; } = false;
    public int HandlerCount { get; private set; } = 0;

    protected override void OnSetupEventHandlers()
    {
        HandlersRegistered = true;
        
        // Register many handlers for performance testing
        for (int i = 0; i < 100; i++)
        {
            var handlerId = i;
            RegisterEventHandler<PerformanceTestEvent>(async @event =>
            {
                await Task.Delay(1); // Simulate some work
                TentativeState.HandlerCalls.Add($"PerformanceHandler{handlerId}: {@event.Id}");
            });
        }
        
        HandlerCount = 100;
    }

    public Task RaisePerformanceEvent(PerformanceTestEvent @event)
    {
        Raise(@event);
        return Task.CompletedTask;
    }

    public Task<TestState> GetState()
    {
        return Task.FromResult(TentativeState);
    }

    public Task<bool> GetHandlersRegistered()
    {
        return Task.FromResult(HandlersRegistered);
    }

    public Task<int> GetRegisteredHandlerCount()
    {
        return Task.FromResult(HandlerCount);
    }
}

/// <summary>
/// Test grain for testing handler registration order.
/// </summary>
public class OrderTestGrain : EventSourcedGrain<TestState, object>, IOrderTestGrain
{
    protected override void OnSetupEventHandlers()
    {
        // Register handlers in specific order
        RegisterEventHandler<OrderTestEvent>(HandleFirst);
        RegisterEventHandler<OrderTestEvent>(HandleSecond);
        RegisterEventHandler<OrderTestEvent>(HandleThird);
    }

    private Task HandleFirst(OrderTestEvent @event)
    {
        TentativeState.HandlerCalls.Add("First");
        return Task.CompletedTask;
    }

    private Task HandleSecond(OrderTestEvent @event)
    {
        TentativeState.HandlerCalls.Add("Second");
        return Task.CompletedTask;
    }

    private Task HandleThird(OrderTestEvent @event)
    {
        TentativeState.HandlerCalls.Add("Third");
        return Task.CompletedTask;
    }

    public Task RaiseOrderTestEvent(OrderTestEvent @event)
    {
        Raise(@event);
        return Task.CompletedTask;
    }

    public Task<List<string>> GetHandlerCalls()
    {
        return Task.FromResult(new List<string>(TentativeState.HandlerCalls));
    }
}
