namespace Strata.Journaling.Tests;

[GenerateSerializer]
public class AccountAggregate : IAggregate
{
    public void Apply(BalanceAdjustedEvent @event)
    {
        Balance = @event.Balance;
    }

    [Id(0)]
    public string Id { get; set; } = null!;

    [Id(1)]
    public int Version { get; set; }

    [Id(2)]
    public double Balance { get; set; }
}
