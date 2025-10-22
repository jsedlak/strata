using Strata.Journaling.Tests.JournalingTests.GrainModel;

namespace Strata.Journaling.Tests.JournalingTests;

public class JournaledGrainTests(JournalingTestFixture fixture) : IClassFixture<JournalingTestFixture>
{
    private IGrainFactory Client => fixture.Client;

    [Fact]
    public async Task JournaledGrain_StateCanSave()
    {
        var grain = Client.GetGrain<IAccountGrain>("account_save_state");

        await grain.Deposit(100);
        await grain.Deactivate();

        await Task.Delay(1000);

        var grain2 = Client.GetGrain<IAccountGrain>("account_save_state");

        var balance = await grain2.GetBalance();
        Assert.Equal(100, balance);

        var log = await grain2.GetEvents();
        Assert.Single(log);
    }

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

        var log = await grain.GetEvents();
        Assert.Equal(2, log.Length);

        var projectionGrain = Client.GetGrain<IAccountViewModelGrain>("testaccount");
        var projectedBalance = await projectionGrain.GetBalance();
        Assert.Equal(60, projectedBalance);
    }

    //[Fact(Skip = "Test needs work")]
    //public async Task JournaledGrain_CanHandleEventsNoDeactivation()
    //{
    //    var testId = "events_no_deactivation";
    //    var grain = Client.GetGrain<IAccountGrain>(testId);
        
    //    await grain.Deposit(100);
        
    //    var balance = await grain.GetBalance();
    //    Assert.Equal(100, balance);
        
    //    await grain.Withdraw(40);
        
    //    balance = await grain.GetBalance();
    //    Assert.Equal(60, balance);

    //    var projectionGrain = Client.GetGrain<IAccountViewModelGrain>(testId);

    //    await Task.Delay(1000);

    //    double projectedBalance = await projectionGrain.GetBalance();
    //    Assert.Equal(60, projectedBalance);
    //}

    [Fact]
    public async Task JournaledGrain_MultipleGrains()
    {
        var grain1 = Client.GetGrain<IAccountGrain>("multiple_grains_1");
        var grain2 = Client.GetGrain<IAccountGrain>("multiple_grains_2");

        await grain1.Deposit(100);
        await grain2.Deposit(200);

        var balance1 = await grain1.GetBalance();
        var balance2 = await grain2.GetBalance();

        Assert.Equal(100, balance1);
        Assert.Equal(200, balance2);
    }
}