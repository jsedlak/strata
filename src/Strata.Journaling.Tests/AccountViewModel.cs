namespace Strata.Journaling.Tests;

[GenerateSerializer]
public class AccountViewModel
{
    [Id(0)]
    public double Balance { get; set; }
}