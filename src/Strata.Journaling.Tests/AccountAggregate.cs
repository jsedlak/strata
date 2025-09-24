namespace Strata.Journaling.Tests;

[GenerateSerializer]
public class AccountAggregate : IAggregate
{
    public void Apply(BalanceAdjustedEvent @event)
    {
        Balance = @event.Balance;
    }

    [Id(0)]
    public int Version { get; set; }

    [Id(1)]
    public double Balance { get; set; }
}
