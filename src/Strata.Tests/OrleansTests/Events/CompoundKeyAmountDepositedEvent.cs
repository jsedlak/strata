namespace Strata.Tests.Events;

[GenerateSerializer]
public sealed class CompoundKeyAmountDepositedEvent : CompoundBankAccountEventBase
{
    public CompoundKeyAmountDepositedEvent(string accountId)
        : base(accountId)
    {

    }
    [Id(0)]
    public double Amount { get; set; }
}
