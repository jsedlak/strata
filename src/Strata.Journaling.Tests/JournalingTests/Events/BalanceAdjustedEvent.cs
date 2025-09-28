namespace Strata.Journaling.Tests.JournalingTests.Events;

[GenerateSerializer]
public sealed class BalanceAdjustedEvent : BaseAccountEvent
{
    public BalanceAdjustedEvent(string id) 
        : base(id)
    {
    }

    [Id(0)]
    public double Balance { get; set; }
}
