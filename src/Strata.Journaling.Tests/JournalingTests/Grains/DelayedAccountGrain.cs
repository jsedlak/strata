using Microsoft.Extensions.Logging;
using Strata.Journaling.Tests.JournalingTests.Events;
using Strata.Journaling.Tests.JournalingTests.GrainModel;
using Strata.Journaling.Tests.JournalingTests.Model;
using Strata.Journaling.Tests.JournalingTests.Projections;

namespace Strata.Journaling.Tests.JournalingTests.Grains;

[GrainType("delayed-account")]
internal sealed class DelayedAccountGrain :
    JournaledGrain<AccountAggregate, BaseAccountEvent>,
    IDelayedAccountGrain
{
    private readonly ILogger<IDelayedAccountGrain> _logger;

    public DelayedAccountGrain(ILogger<IDelayedAccountGrain> logger)
    {
        _logger = logger;
    }

    protected override void OnRegisterRecipients()
    {
        RegisterRecipient(
            nameof(DelayedAccountProjection),
            new DelayedAccountProjection(this.GrainFactory, _logger)
        );
    }

    public Task<BaseAccountEvent[]> GetEvents() => Task.FromResult(Log.Select(e => e.Event).ToArray());

    public Task<double> GetBalance() => Task.FromResult(ConfirmedState.Balance);

    public Task<bool> GetIsProcessingOutbox() => Task.FromResult(IsProcessingOutbox);

    public async Task Deposit(double amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Deposit amount must be positive.");
        var newBalance = ConfirmedState.Balance + amount;
        var @event = new BalanceAdjustedEvent(this.GetPrimaryKeyString()) { Balance = newBalance };

        _logger.LogInformation("Depositing {0} to account {1}. New balance will be {2}", amount, this.GetPrimaryKeyString(), newBalance);

        await RaiseEvent(@event);
    }

    public async Task Withdraw(double amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Withdrawal amount must be positive.");
        if (amount > ConfirmedState.Balance) throw new InvalidOperationException("Insufficient funds for withdrawal.");
        var newBalance = ConfirmedState.Balance - amount;
        var @event = new BalanceAdjustedEvent(this.GetPrimaryKeyString()) { Balance = newBalance };

        _logger.LogInformation("Withdrawing {0} from account {1}. New balance will be {2}", amount, this.GetPrimaryKeyString(), newBalance);
        
        await RaiseEvent(@event);
    }
}
