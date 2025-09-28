using Strata.Journaling.Tests.SidecarTests.GrainModel;

namespace Strata.Journaling.Tests.JournalingTests;

public class SidecarTests(SidecarTestFixture fixture) : IClassFixture<SidecarTestFixture>
{
    private IGrainFactory Client => fixture.Client;

    [Fact]
    public async Task Sidecar_ReferenceIdIsSet()
    {
        var grain = Client.GetGrain<IUserGrain>("user1234");

        var data = await grain.GetData();
        Assert.NotNull(data);
        Assert.NotNull(data.ReferenceId);
        Assert.NotEmpty(data.ReferenceId);
    }

}