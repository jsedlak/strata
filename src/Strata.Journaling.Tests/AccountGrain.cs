using Microsoft.Extensions.DependencyInjection;
using Orleans.Journaling;

namespace Strata.Journaling.Tests;

[GrainType("account")]
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

        if (!_state.RecordExists)
        {
            _state.State = new();
            _state.State.Id = this.GetPrimaryKeyString();
            _state.State.Version = 1;

            await WriteStateAsync();
        }

        RegisterRecipient(nameof(AccountProjection), new AccountProjection(this.GrainFactory));
    }

    public ValueTask Deactivate()
    {
        this.DeactivateOnIdle();
        return ValueTask.CompletedTask;
    }

    public Task<BaseAccountEvent[]> GetEvents() => Task.FromResult(Log);

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
