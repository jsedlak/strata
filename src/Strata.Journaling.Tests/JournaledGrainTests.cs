namespace Strata.Journaling.Tests;

public class JournaledGrainTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    private IGrainFactory Client => fixture.Client;

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

        await grain.Deactivate();

        var grain2 = Client.GetGrain<IAccountGrain>("testaccount");

        var storedBalance = await grain2.GetBalance();
        Assert.Equal(60, storedBalance);

        var events = await grain2.GetEvents();
        Assert.Equal(2, events.Length);

        var projectionGrain = Client.GetGrain<IAccountViewModelGrain>("testaccount");
        var projectedBalance = await projectionGrain.GetBalance();
        Assert.Equal(60, projectedBalance);
    }

    [Fact]
    public async Task JournaledGrain_MultipleGrains()
    {
        var grain1 = Client.GetGrain<IAccountGrain>(Guid.NewGuid().ToString());
        var grain2 = Client.GetGrain<IAccountGrain>(Guid.NewGuid().ToString());

        await grain1.Deposit(100);
        await grain2.Deposit(200);

        var balance1 = await grain1.GetBalance();
        var balance2 = await grain2.GetBalance();

        Assert.Equal(100, balance1);
        Assert.Equal(200, balance2);
    }
}