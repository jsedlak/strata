using Orleans;

namespace Strata.Tests.Events;

[GenerateSerializer]
public sealed class AmountDepositedEvent : BankAccountEventBase
{
    public AmountDepositedEvent(Guid accountId)
        : base(accountId)
    {
    }

    [Id(0)]
    public double Amount { get; set; }
}
