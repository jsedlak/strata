![Strata Logo](img/strata-logo.png)

# Strata

Strata is an opinionated Event Sourcing library built for Microsoft Orleans.

## Goals

- ♾️ Tentative & Confirmed State Models & Versioning
- 📷 Snapshotting
- ⏱️ Delayed Writes
- 🗃️ Orleans Provider Model via Durable Framework
- ⚡ Outbox Recipients & Projections

## Examples

### Build an Aggregate Model

Create an aggregate object that can handle events being applied to it.

```csharp
[GenerateSerializer]
public class AccountAggregate : IAggregate
{
    public void Apply(BalanceAdjustedEvent @event)
    {
        Balance = @event.Balance;
    }

    [Id(0)]
    public string Id { get; set; } = null!;

    [Id(1)]
    public int Version { get; set; }

    [Id(2)]
    public double Balance { get; set; }
}
```

### Build an Aggregate Grain

Implement a Journaled Grain that converts commands into events.


```csharp
internal sealed class AccountGrain : 
    JournaledGrainBase<AccountAggregate, BaseAccountEvent>, 
    IAccountGrain
{

    private readonly IPersistentState<AccountAggregate> _state;

    public AccountGrain(
        [FromKeyedServices("log")] IDurableList<BaseAccountEvent> eventLog,
        [FromKeyedServices("outbox")] IDurableQueue<OutboxEnvelope<BaseAccountEvent>> outbox,
        [FromKeyedServices("state")] IPersistentState<AccountAggregate> state
    ) : base(eventLog, outbox, state)
    {
        _state = state;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
    
        if(!_state.RecordExists)
        {
            _state.State = new();
            _state.State.Id = this.GetPrimaryKeyString();
            _state.State.Version = 1;

            await WriteStateAsync();
        }
    }

    public Task<double> GetBalance() => Task.FromResult(ConfirmedState.Balance);

    public async Task Deposit(double amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Deposit amount must be positive.");
        var newBalance = ConfirmedState.Balance + amount;
        var @event = new BalanceAdjustedEvent(this.GetPrimaryKeyString()) { Balance = newBalance };
        await RaiseEvent(@event);
    }

    public async Task Withdraw(double amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Withdrawal amount must be positive.");
        if (amount > ConfirmedState.Balance) throw new InvalidOperationException("Insufficient funds for withdrawal.");
        var newBalance = ConfirmedState.Balance - amount;
        var @event = new BalanceAdjustedEvent(this.GetPrimaryKeyString()) { Balance = newBalance };
        await RaiseEvent(@event);
    }
}
```

### Handle Outbox Messages

Register an `IOutboxRecipient<TEvent>` to handle outbox messages in the `OnActivateAsync` method of your grain. This allows you to act on events, connecting other grains or passing the message to a stream or bus.

```csharp
RegisterRecipient(nameof(AccountProjection), new AccountProjection(this.GrainFactory));


public sealed class AccountProjection : IOutboxRecipient<BaseAccountEvent>
{
    private readonly IGrainFactory _grainFactory;

    public AccountProjection(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public async Task Handle(int version, BaseAccountEvent @event)
    {
        if (@event is BalanceAdjustedEvent balanceEvent)
        {
            var accountId = balanceEvent.Id;
            var viewModelGrain = _grainFactory.GetGrain<IAccountViewModelGrain>(accountId);
            await viewModelGrain.UpdateBalance(balanceEvent.Balance);
        }
    }
}
```