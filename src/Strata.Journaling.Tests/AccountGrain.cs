using Microsoft.Extensions.DependencyInjection;
using Orleans.Journaling;

namespace Strata.Journaling.Tests;

internal sealed class AccountGrain : 
    JournaledGrainBase<AccountAggregate, BaseAccountEvent>, 
    IAccountGrain
{
    public AccountGrain(
        [FromKeyedServices("log")] IDurableList<BaseAccountEvent> eventLog,
        [FromKeyedServices("outbox")] IDurableQueue<OutboxEnvelope<BaseAccountEvent>> outbox,
        [FromKeyedServices("state")] IPersistentState<AccountAggregate> state
    ) : base(eventLog, outbox, state)
    {
    }

    public Task<double> GetBalance() => Task.FromResult(ConfirmedState.Balance);

    public async Task Deposit(double amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Deposit amount must be positive.");
        var newBalance = ConfirmedState.Balance + amount;
        var @event = new BalanceAdjustedEvent { Balance = newBalance };
        await RaiseEvent(@event);
    }

    public async Task Withdraw(double amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Withdrawal amount must be positive.");
        if (amount > ConfirmedState.Balance) throw new InvalidOperationException("Insufficient funds for withdrawal.");
        var newBalance = ConfirmedState.Balance - amount;
        var @event = new BalanceAdjustedEvent { Balance = newBalance };
        await RaiseEvent(@event);
    }
}