//using Strata.Journaling.Tests.SidecarTests.GrainModel;
//using Strata.Sidecars;

//namespace Strata.Journaling.Tests.SidecarTests.Grains;

//[GrainType("user-cdp")]
//internal sealed class UserCdpGrain : Grain, IUserCdpGrain
//{
//    public async Task InitializeSidecar()
//    {
//        // set a reminder, do some work, get the data, etc.

//        var userGrain = GrainFactory.GetGrain<IUserGrain>(
//            this.GetPrimaryKeyString()
//        );

//        await userGrain.SetReferenceId(Guid.NewGuid().ToString());
//        await userGrain.DisableSidecar<IUserCdpGrain>();
//    }
//}
