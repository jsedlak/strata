namespace Strata.Journaling.Tests;

[GenerateSerializer]
public sealed class BalanceAdjustedEvent : BaseAccountEvent
{
    [Id(0)]
    public double Balance { get; set; }
}
