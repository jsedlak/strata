# Strata Event Handlers

This document describes event handlers, a dynamic approach to handling events from within an `EventSourcedGrain`. Event handlers provide a simple, delegate-based mechanism for processing events as they are raised within an event-sourced grain.

## Overview

Event handlers allow you to register custom logic that executes whenever events are raised in an `EventSourcedGrain`. Handlers are called in the order they were registered, and if one handler fails, processing continues with the remaining handlers.

## Registering Event Handlers

Event handlers are registered during grain setup by overriding the `OnSetupEventHandlers` method. This method is called after the event handler registry is initialized but before the grain is activated.

### Typed Event Handlers

Register handlers for specific event types using the generic `RegisterEventHandler<TEvent>` method:

```csharp
public class BankAccountGrain : EventSourcedGrain<BankAccount, BankAccountEventBase>
{
    protected override void OnSetupEventHandlers()
    {
        // Handle specific event types
        RegisterEventHandler<AmountDepositedEvent>(async @event =>
        {
            // Process deposit event
            await ProcessDeposit(@event.Amount);
            TentativeState.LastDepositTime = DateTime.UtcNow;
        });

        RegisterEventHandler<AmountWithdrawnEvent>(async @event =>
        {
            // Process withdrawal event
            await ProcessWithdrawal(@event.Amount);
            TentativeState.LastWithdrawalTime = DateTime.UtcNow;
        });
    }
}
```

### Untyped Event Handlers

Register handlers that process all events using the non-generic `RegisterEventHandler` method:

```csharp
protected override void OnSetupEventHandlers()
{
    // Handle all events
    RegisterEventHandler(async @event =>
    {
        // Log all events
        await LogEvent(@event);

        // Update audit trail
        TentativeState.AuditTrail.Add(new AuditEntry
        {
            EventType = @event.GetType().Name,
            Timestamp = DateTime.UtcNow
        });
    });
}
```

### Mixed Handler Registration

You can register both typed and untyped handlers in the same grain:

```csharp
protected override void OnSetupEventHandlers()
{
    // Typed handlers for specific events
    RegisterEventHandler<OrderCreatedEvent>(HandleOrderCreated);
    RegisterEventHandler<OrderCancelledEvent>(HandleOrderCancelled);

    // Untyped handler for all events
    RegisterEventHandler(HandleAllEvents);
}

private async Task HandleOrderCreated(OrderCreatedEvent @event)
{
    // Specific logic for order creation
    await NotifyOrderCreated(@event);
}

private async Task HandleOrderCancelled(OrderCancelledEvent @event)
{
    // Specific logic for order cancellation
    await ProcessRefund(@event);
}

private async Task HandleAllEvents(object @event)
{
    // Common logic for all events
    await UpdateMetrics(@event);
}
```

## Handler Execution

When `Raise` is called on an `EventSourcedGrain`, the following sequence occurs:

1. **Handler Processing**: All registered handlers for the event type are called in registration order
2. **Event Submission**: The event is submitted to the event log
3. **Confirmation**: If `_saveOnRaise` is true, the grain waits for confirmation

### Execution Order

Handlers are executed in the order they were registered:

```csharp
protected override void OnSetupEventHandlers()
{
    // This handler executes first
    RegisterEventHandler<PaymentEvent>(async @event =>
    {
        await LogPayment(@event);
    });

    // This handler executes second
    RegisterEventHandler<PaymentEvent>(async @event =>
    {
        await UpdateBalance(@event);
    });

    // This handler executes third
    RegisterEventHandler(async @event =>
    {
        await NotifyExternalSystems(@event);
    });
}
```

### Error Handling

By default, if one handler fails, processing continues with the remaining handlers:

```csharp
protected override void OnSetupEventHandlers()
{
    RegisterEventHandler<PaymentEvent>(async @event =>
    {
        // This might fail, but won't stop other handlers
        await ExternalPaymentService.Process(@event);
    });

    RegisterEventHandler<PaymentEvent>(async @event =>
    {
        // This will still execute even if the above fails
        await UpdateLocalState(@event);
    });
}
```

## Configuration Options

Event handler behavior can be configured through the `EventHandlerOptions` class:

### Basic Configuration

```csharp
// In your service configuration
services.Configure<EventHandlerOptions>(options =>
{
    options.FailFastOnHandlerError = false;  // Default: false
    options.LogHandlerExecution = true;      // Default: true
    options.LogHandlerErrors = true;         // Default: true
    options.MaxHandlerExecutionTime = TimeSpan.FromSeconds(30); // Default: null (no timeout)
});
```

### Configuration Options

| Option                    | Type        | Default | Description                                            |
| ------------------------- | ----------- | ------- | ------------------------------------------------------ |
| `FailFastOnHandlerError`  | `bool`      | `false` | If true, stops processing when a handler fails         |
| `LogHandlerExecution`     | `bool`      | `true`  | Enables logging of handler execution                   |
| `LogHandlerErrors`        | `bool`      | `true`  | Enables logging of handler errors                      |
| `MaxHandlerExecutionTime` | `TimeSpan?` | `null`  | Maximum execution time per handler (null = no timeout) |

### Fail-Fast Configuration

For critical applications where handler failures should stop event processing:

```csharp
services.Configure<EventHandlerOptions>(options =>
{
    options.FailFastOnHandlerError = true;
});
```

### Timeout Configuration

To prevent handlers from running indefinitely:

```csharp
services.Configure<EventHandlerOptions>(options =>
{
    options.MaxHandlerExecutionTime = TimeSpan.FromSeconds(10);
});
```

## Advanced Usage Patterns

### Conditional Handler Registration

```csharp
protected override void OnSetupEventHandlers()
{
    if (TentativeState.IsPremiumAccount)
    {
        RegisterEventHandler<PaymentEvent>(async @event =>
        {
            await ProcessPremiumPayment(@event);
        });
    }

    RegisterEventHandler<PaymentEvent>(async @event =>
    {
        await ProcessStandardPayment(@event);
    });
}
```

### Handler with State Validation

```csharp
protected override void OnSetupEventHandlers()
{
    RegisterEventHandler<WithdrawalEvent>(async @event =>
    {
        if (TentativeState.Balance < @event.Amount)
        {
            throw new InsufficientFundsException();
        }

        await ProcessWithdrawal(@event);
    });
}
```

### Async Handler with External Dependencies

```csharp
protected override void OnSetupEventHandlers()
{
    RegisterEventHandler<OrderEvent>(async @event =>
    {
        // Call external service
        await _notificationService.SendOrderUpdate(@event);

        // Update local state
        TentativeState.LastOrderUpdate = DateTime.UtcNow;
    });
}
```

## Performance Considerations

### Handler Efficiency

- Keep handlers lightweight and fast
- Use async/await for I/O operations
- Avoid blocking operations in handlers
- Consider using background tasks for heavy processing

### Memory Management

- Handlers are automatically cleaned up when the grain is deactivated
- Avoid capturing large objects in handler closures
- Use dependency injection for shared services

### Error Handling Best Practices

```csharp
protected override void OnSetupEventHandlers()
{
    RegisterEventHandler<CriticalEvent>(async @event =>
    {
        try
        {
            await ProcessCriticalEvent(@event);
        }
        catch (Exception ex)
        {
            // Log the error but don't rethrow to avoid stopping other handlers
            _logger.LogError(ex, "Failed to process critical event");

            // Optionally, store the error for later processing
            TentativeState.PendingErrors.Add(new ErrorRecord
            {
                Event = @event,
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    });
}
```

## Testing Event Handlers

### Unit Testing

```csharp
[Test]
public async Task EventHandler_ProcessesEventCorrectly()
{
    var grain = _grainFactory.GetGrain<IBankAccountGrain>(Guid.NewGuid());

    // Raise an event
    await grain.Deposit(new DepositCommand { Amount = 100 });

    // Verify handler effects
    var state = await grain.GetBalance();
    Assert.AreEqual(100, state);
}
```

### Integration Testing

```csharp
[Test]
public async Task EventHandlers_ExecuteInCorrectOrder()
{
    var grain = _grainFactory.GetGrain<ITestGrain>(Guid.NewGuid());

    await grain.RaiseTestEvent(new TestEvent { Message = "Test" });

    var handlerCalls = await grain.GetHandlerCalls();
    Assert.AreEqual(3, handlerCalls.Count);
    Assert.AreEqual("First", handlerCalls[0]);
    Assert.AreEqual("Second", handlerCalls[1]);
    Assert.AreEqual("Third", handlerCalls[2]);
}
```

## Migration Guide

### From Manual Event Processing

**Before:**

```csharp
public ValueTask<double> Deposit(DepositCommand command)
{
    var @event = new AmountDepositedEvent(GetPrimaryKey()) { Amount = command.Amount };

    // Manual processing
    TentativeState.Balance += @event.Amount;
    TentativeState.LastDepositTime = DateTime.UtcNow;

    Raise(@event);
    return ValueTask.FromResult(TentativeState.Balance);
}
```

**After:**

```csharp
public ValueTask<double> Deposit(DepositCommand command)
{
    var @event = new AmountDepositedEvent(GetPrimaryKey()) { Amount = command.Amount };

    Raise(@event); // Handlers will process the event
    return ValueTask.FromResult(TentativeState.Balance);
}

protected override void OnSetupEventHandlers()
{
    RegisterEventHandler<AmountDepositedEvent>(async @event =>
    {
        TentativeState.Balance += @event.Amount;
        TentativeState.LastDepositTime = DateTime.UtcNow;
    });
}
```

## Troubleshooting

### Common Issues

1. **Handlers not executing**: Ensure `OnSetupEventHandlers` is overridden and handlers are registered
2. **Handlers executing multiple times**: Check for duplicate registrations
3. **Performance issues**: Review handler complexity and consider async patterns
4. **Memory leaks**: Ensure handlers don't capture large objects or create circular references

### Debugging

Enable detailed logging to troubleshoot handler execution:

```csharp
services.Configure<EventHandlerOptions>(options =>
{
    options.LogHandlerExecution = true;
    options.LogHandlerErrors = true;
});
```

This will log handler execution details and any errors that occur during processing.
