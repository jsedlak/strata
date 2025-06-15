using Strata.Tests.Events;

namespace Strata.Tests.Model;

public sealed class StringKeyBankAccount
{
    public double Balance { get; set; }

    public void Apply(CompoundKeyAmountDepositedEvent @event)
    {
        Balance += @event.Amount;
    }

    public void Apply(CompoundKeyAmountWithdrawnEvent @event)
    {
        Balance -= @event.Amount;
    }
}