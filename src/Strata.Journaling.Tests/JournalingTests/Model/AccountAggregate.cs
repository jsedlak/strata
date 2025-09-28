using Strata.Journaling.Tests.JournalingTests.Events;

namespace Strata.Journaling.Tests.JournalingTests.Model;

[GenerateSerializer]
public class AccountAggregate
{
    public void Apply(BalanceAdjustedEvent @event)
    {
        Balance = @event.Balance;
    }

    [Id(0)]
    public string Id { get; set; } = null!;

    [Id(1)]
    public double Balance { get; set; }
}
