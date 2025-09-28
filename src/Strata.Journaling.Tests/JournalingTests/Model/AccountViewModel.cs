namespace Strata.Journaling.Tests.JournalingTests.Model;

[GenerateSerializer]
public class AccountViewModel
{
    [Id(0)]
    public double Balance { get; set; }
}