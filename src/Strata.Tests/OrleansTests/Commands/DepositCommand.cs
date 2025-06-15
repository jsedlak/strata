using Orleans;

namespace Strata.Tests.Commands;

[GenerateSerializer]
public class DepositCommand
{
    [Id(0)]
    public double Amount { get; set; }
}