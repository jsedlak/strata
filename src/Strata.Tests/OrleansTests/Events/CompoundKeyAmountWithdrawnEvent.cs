namespace Strata.Tests.Events;

[GenerateSerializer]
public sealed class CompoundKeyAmountWithdrawnEvent : CompoundBankAccountEventBase
{
    public CompoundKeyAmountWithdrawnEvent(string accountId) 
        : base(accountId)
    {
    }
    
    [Id(0)]
    public double Amount { get; set; }
}