using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Journaling;
using Strata.Journaling.Tests.JournalingTests.Events;
using Strata.Journaling.Tests.JournalingTests.GrainModel;
using Strata.Journaling.Tests.JournalingTests.Model;
using Strata.Journaling.Tests.JournalingTests.Projections;

namespace Strata.Journaling.Tests.JournalingTests.Grains;

[GrainType("account")]
internal sealed class AccountGrain :
    JournaledGrain<AccountAggregate, BaseAccountEvent>,
    IAccountGrain
{
    private readonly ILogger<IAccountGrain> _logger;




    public AccountGrain(ILogger<IAccountGrain> logger)
    {
        _logger = logger;
    }

    protected override void OnRegisterRecipients()
    {
        RegisterRecipient(
            nameof(AccountProjection),
            new AccountProjection(this.GrainFactory, _logger)
        );
    }

    public ValueTask Deactivate()
    {
        this.DeactivateOnIdle();
        return ValueTask.CompletedTask;
    }

    public Task<BaseAccountEvent[]> GetEvents() => Task.FromResult(Log.Select(e => e.Event).ToArray());

    public Task<double> GetBalance() => Task.FromResult(ConfirmedState.Balance);

    public async Task Deposit(double amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Deposit amount must be positive.");
        var newBalance = ConfirmedState.Balance + amount;
        var @event = new BalanceAdjustedEvent(this.GetPrimaryKeyString()) { Balance = newBalance };
        await RaiseEvent(@event);

        var vmg = GrainFactory.GetGrain<IAccountViewModelGrain>(this.GetPrimaryKeyString());
        await vmg.UpdateBalance(newBalance);
    }

    public async Task Withdraw(double amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Withdrawal amount must be positive.");
        if (amount > ConfirmedState.Balance) throw new InvalidOperationException("Insufficient funds for withdrawal.");
        var newBalance = ConfirmedState.Balance - amount;
        var @event = new BalanceAdjustedEvent(this.GetPrimaryKeyString()) { Balance = newBalance };
        await RaiseEvent(@event);
        var vmg = GrainFactory.GetGrain<IAccountViewModelGrain>(this.GetPrimaryKeyString());
        await vmg.UpdateBalance(newBalance);
    }
}
