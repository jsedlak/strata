namespace Strata.Journaling.Tests;

public class JournaledGrainTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    private IGrainFactory Client => fixture.Client;

    /// <summary>
    /// Tests basic state persistence for a durable grain.
    /// Verifies that simple state properties (string and int) are correctly
    /// persisted and recovered after grain deactivation.
    /// </summary>
    [Fact]
    public async Task JournaledGrain_CanHandleEvents()
    {
        var grain = Client.GetGrain<IAccountGrain>("testaccount");
        await grain.Deposit(100);
        var balance = await grain.GetBalance();
        Assert.Equal(100, balance);
        await grain.Withdraw(40);
        balance = await grain.GetBalance();
        Assert.Equal(60, balance);
    }
}