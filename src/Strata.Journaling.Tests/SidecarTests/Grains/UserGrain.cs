//using Microsoft.Extensions.DependencyInjection;
//using Strata.Journaling.Tests.SidecarTests.GrainModel;
//using Strata.Journaling.Tests.SidecarTests.Model;
//using Strata.Sidecars;

//namespace Strata.Journaling.Tests.SidecarTests.Grains;

//[GrainType("user")]
//internal sealed class UserGrain : Grain, IUserGrain,
//    ISidecarHost<IUserCdpGrain>
//{
//    private readonly IPersistentState<UserData> _state;

//    public UserGrain(
//        [FromKeyedServices("state")] IPersistentState<UserData> state
//    )
//    {
//        _state = state;
//    }

//    public Task<UserData> GetData() =>
//        Task.FromResult(_state.State);

//    public async ValueTask SetName(string name)
//    {
//        _state.State.Name = name;
//        await _state.WriteStateAsync();
//    }

//    public async ValueTask SetReferenceId(string referenceId)
//    {
//        _state.State.ReferenceId = referenceId;
//        await _state.WriteStateAsync();

//    }
//}
