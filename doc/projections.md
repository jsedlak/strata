# Projections

A Projection is a process by which data from an Event is used to alter the View Model or downstream system. In the simplest way, a Projection is the ability to execute code asynchronously as the result of an Event occuring.

For any grain that utilizes the Strata base, either `StreamingEventSourcedGrain` or `EventSourcedGrain` there is the ability to opt-in for automatic projections.

Consider the `Account` model as follows.

```csharp
internal class Account : IAggregate
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public double Balance { get; set; }

    public void Apply(AmountAddedEvent ev)
    {
        Balance += ev.Amount;
    }

    public void Apply(AmountRemovedEvent ev)
    {
        Balance -= ev.Amount;
    }
}
```

In this case, we have defined a domain model that may receive two events; one event to add money to the account, and another to subtract.

Note that this class is a Domain Model and is marked internal such that it cannot be exposed or otherwise passed to the front end. We may need to store a collection of these (for a particular owner) or simply return a view of this data for the owner to see.

```csharp
public sealed class AccountViewModel
{
    public string Id { get; set; }

    public double Balance { get; set; }
}
```

In the case of a view model, it is a POCO that represents the latest state of the Account in a way that is meant for a specific front-end use case. There may be many such view models, each with its own set of properties and use cases.

## Adding a Projection

To support keeping this View Model up-to-date, we add a Projection class.

```csharp
public sealed class AccountViewModelProjection :
    IProjection<AmountAddedEvent>,
    IProjection<AmountRemovedEvent>
{
    public async Task Handle(AmountAddedEvent @event)
    {
        // ... do something with the event
    }

    public async Task Handle(AmountRemovedEvent @event)
    {
        // ... do something with the event
    }
}
```

We then register this event with our Domain Model Grain.

```csharp
internal sealed class AccountGrain :
    EventSourcedGrain<Account, BaseAccountEvent> {

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        this.RegisterProjection<AccountViewModelProjection>();

        return base.OnActivateAsync(cancellationToken);
    }
}
```

This registers an event handler that will be called whenever the `RaiseEvent` methods are called, receiving the event(s) as a result. The internal mechanism of Strata's EventSourcedGrain will need to be updated to support such event handler registrations, via a protected `RegisterEventHandler` method.

## Projection Grains

For each type that is passed into the `RegisterProjection` method, a `ProjectionGrain` is spun up. The `ProjectionGrain` does a few things to help offload the work from the Domain Model Grain (`AccountGrain`).

1. This Grain is identified by the type of projection via a compound string key based on your grain's ID: "{grain id}/{projection type}"
2. The Grain implements `IProjectionGrain` and uses an internal queue mechanism to ensure that work is ordered.
3. When this Grain's `Apply` method is called, the event is queued internally. It then uses a worker thread to manage processing of the queue. If the internal worker thread is not running, it is spun up immediately.
4. When the internal thread is initialized, the queue is cleared and all work is processed for that instance of the thread.
5. When the internal worker task is completed, the queue is checked and if any new work is present, the process starts again.

### Reference: IProjectionGrain

```csharp
public interface IProjectionGrain : IGrainWithStringKey
{
    Task Apply(object @event);
}
```

# Relevant Classes

- Strata.Projections.IProjectionGrain
- Strata.Projections.ProjectionGrain
- Strata.Projections.IProjection<TEvent>
- Strata.Projections.GrainExtensions
