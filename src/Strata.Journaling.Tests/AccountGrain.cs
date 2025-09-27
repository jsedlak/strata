namespace Strata.Journaling.Tests;

[GrainType("account")]
internal sealed class AccountGrain :
    JournaledGrain<AccountAggregate, BaseAccountEvent>,
    IAccountGrain
{
    protected override void OnRegisterRecipients()
    {
        RegisterRecipient(nameof(AccountProjection), new AccountProjection(this.GrainFactory));
    }

    public ValueTask Deactivate()
    {
        this.DeactivateOnIdle();
        return ValueTask.CompletedTask;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        Console.WriteLine("[{0}] OnActivateAsync", this.GetPrimaryKeyString());
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await base.OnDeactivateAsync(reason, cancellationToken);
        Console.WriteLine("[{0}] OnDeactivateAsync", this.GetPrimaryKeyString());
    }

    public Task<BaseAccountEvent[]> GetEvents() => Task.FromResult(Log.Select(e => e.Event).ToArray());

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
