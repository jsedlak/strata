namespace Strata.Journaling.Tests;

public class JournaledGrainActivationTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    private IGrainFactory Client => fixture.Client;

    [Fact(Skip = "This test is not necessary to execute every time. It may be useful for making sure a grain is deactivated on idle despite using an infinite timer.")]
    public async Task JournaledGrain_CanDeactivateOnIdle()
    {
        var grain1 = Client.GetGrain<IAccountGrain>("multiple_grains_1");
        var grain2 = Client.GetGrain<IAccountGrain>("multiple_grains_2");

        await grain1.Deposit(100);
        await grain2.Deposit(200);

        var balance1 = await grain1.GetBalance();
        var balance2 = await grain2.GetBalance();

        Assert.Equal(100, balance1);
        Assert.Equal(200, balance2);


        var mgmt = Client.GetGrain<IManagementGrain>(0);
        var hosts = await mgmt.GetHosts(true);

        await mgmt.ForceGarbageCollection(hosts.Keys.ToArray());

        var activeGrainIds = await mgmt.GetActiveGrains(GrainType.Create("account"));
        var startTime = DateTime.UtcNow;
        while (activeGrainIds.Count > 0)
        {
            await Task.Yield();
            await Task.Delay(TimeSpan.FromSeconds(1));

            activeGrainIds = await mgmt.GetActiveGrains(GrainType.Create("account"));

            if (DateTime.UtcNow - startTime > TimeSpan.FromMinutes(3))
            {
                //throw new Exception("Timeout, grains did not deactivate");
                Assert.Fail("Timeout, grains did not deactivate");
                break;
            }
        }
    }
}