# Strata Projections

The Strata Projections feature provides a powerful way to process events asynchronously in your event-sourced applications. It supports two main approaches: **Host Managed Projections** and **Projection Managed Grains**.

## Features

- **Async Processing**: Projections are processed asynchronously without blocking the main event flow
- **Type Safety**: Strongly-typed event handling with compile-time type checking
- **Orleans Integration**: Built on top of Microsoft Orleans for distributed processing
- **Flexible Configuration**: Configurable concurrency, timeouts, and retry policies
- **Error Handling**: Comprehensive error handling with retry logic and dead letter queue support
- **Performance Monitoring**: Built-in performance counters and metrics

## Quick Start

### 1. Install the Package

```bash
dotnet add package Strata
```

### 2. Configure Services

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddProjections(options =>
    {
        options.MaxConcurrency = 20;
        options.ProcessingTimeoutMs = 60000;
        options.MaxRetryAttempts = 5;
        options.EnableDeadLetterQueue = true;
    });
}
```

### 3. Create a Projection

```csharp
public class AccountBalanceProjection : IProjection<AmountDepositedEvent>, IProjection<AmountWithdrawnEvent>
{
    private decimal _balance;

    public async Task Handle(AmountDepositedEvent @event)
    {
        _balance += @event.Amount;
        // Update your read model or send notifications
    }

    public async Task Handle(AmountWithdrawnEvent @event)
    {
        _balance -= @event.Amount;
        // Update your read model or send notifications
    }
}
```

### 4. Register with EventSourcedGrain

```csharp
public class BankAccountGrain : EventSourcedGrain
{
    public BankAccountGrain(ILogger<BankAccountGrain> logger) : base(logger)
    {
        // Register projections
        this.RegisterProjection<AccountBalanceProjection>();
    }

    public async Task Deposit(decimal amount)
    {
        var @event = new AmountDepositedEvent { Amount = amount };
        await Raise(@event);
    }
}
```

## Projection Types

### Host Managed Projections

Host Managed Projections are processed by the `ProjectionGrain` using Orleans OneWay calls. They are ideal for:

- Simple event processing
- Stateless projections
- High-throughput scenarios
- When you want the host to manage projection lifecycle

```csharp
// Register a host managed projection
this.RegisterProjection<AccountBalanceProjection>();
```

### Projection Managed Grains

Projection Managed Grains use Orleans streams for event processing. They are ideal for:

- Stateful projections
- Complex event processing logic
- When you need fine-grained control over projection lifecycle
- Stream-based event processing

```csharp
[ImplicitStreamSubscription("AccountStream")]
public class StreamBasedProjectionGrain : EventRecipientGrain, IStreamBasedProjectionGrain
{
    public async Task Handle(AmountDepositedEvent @event)
    {
        // Process event from stream
    }
}
```

## Configuration Options

The `ProjectionOptions` class provides comprehensive configuration:

```csharp
public class ProjectionOptions
{
    public int MaxConcurrency { get; set; } = 10;           // Max concurrent projections
    public int ProcessingTimeoutMs { get; set; } = 30000;   // Processing timeout
    public int MaxRetryAttempts { get; set; } = 3;          // Max retry attempts
    public int RetryDelayMs { get; set; } = 1000;           // Delay between retries
    public bool EnableDeadLetterQueue { get; set; } = true; // Enable dead letter queue
    public int MaxQueueSize { get; set; } = 10000;          // Max queue size
    public bool EnablePerformanceCounters { get; set; } = true; // Enable metrics
    public int BatchSize { get; set; } = 10;                // Batch processing size
}
```

## Error Handling

Projections include comprehensive error handling:

- **Retry Logic**: Configurable retry attempts with exponential backoff
- **Error Isolation**: Projection failures don't affect main event processing
- **Dead Letter Queue**: Failed events can be sent to a dead letter queue
- **Logging**: Detailed logging for troubleshooting

## Performance Considerations

- **Async Processing**: All projections are processed asynchronously
- **Concurrency Control**: Configurable concurrency limits prevent resource exhaustion
- **Batch Processing**: Events are processed in batches for efficiency
- **Memory Management**: Efficient memory usage with object pooling where appropriate

## Best Practices

1. **Keep Projections Simple**: Projections should be focused on a single responsibility
2. **Handle Errors Gracefully**: Always handle exceptions in projection methods
3. **Use Appropriate Projection Type**: Choose Host Managed for simple cases, Projection Managed for complex ones
4. **Monitor Performance**: Use the built-in performance counters to monitor projection performance
5. **Test Thoroughly**: Write comprehensive tests for your projections

## Examples

See the `examples/ProjectionExamples.cs` file for complete working examples of:

- Simple projections
- Complex multi-event projections
- Stream-based projections
- Service configuration
- Error handling patterns

## Testing

The projections feature includes comprehensive test coverage:

- Unit tests for all core components
- Integration tests with Orleans
- Performance tests for load scenarios
- Example tests demonstrating usage patterns

Run the tests with:

```bash
dotnet test
```

## Troubleshooting

### Common Issues

1. **Projection Not Processing**: Check that the projection is properly registered and implements the correct interfaces
2. **Performance Issues**: Review concurrency settings and batch sizes
3. **Memory Leaks**: Ensure projections don't hold references to large objects
4. **Orleans Errors**: Check Orleans configuration and grain activation

### Debugging

Enable detailed logging to troubleshoot issues:

```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

## Contributing

Contributions are welcome! Please see the main Strata repository for contribution guidelines.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
