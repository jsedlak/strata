using Orleans;

namespace Strata.Tests.Commands;

[GenerateSerializer]
public class WithdrawCommand
{
    [Id(0)]
    public double Amount { get; set; }
}