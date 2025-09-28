namespace Strata.Sidecars;

internal sealed class SidecarLifecycleParticipant<TGrain, TSidecar> 
    : ILifecycleParticipant<IGrainLifecycle>
    where TGrain : Grain, ISidecarHost<TSidecar>
    where TSidecar : class, ISidecarGrain
{
    private readonly TGrain _grain;
    private readonly IPersistentState<SidecarState> _sidecarState;
    private readonly IGrainFactory _grainFactory;

    public SidecarLifecycleParticipant(
        TGrain grain,
        [PersistentState("sidecar", "sidecarStore")] IPersistentState<SidecarState> sidecarState,
        IGrainFactory grainFactory)
    {
        _grain = grain;
        _sidecarState = sidecarState;
        _grainFactory = grainFactory;
    }

    public void Participate(IGrainLifecycle lifecycle)
    {
        lifecycle.Subscribe(
            nameof(SidecarLifecycleParticipant<TGrain, TSidecar>),
            GrainLifecycleStage.Activate,
            OnActivateAsync,
            OnDeactivateAsync);
    }

    private async Task OnActivateAsync(CancellationToken ct)
    {
        /*
        await _grain.AddExtensionAsync<ISidecarControlExtension>(
            () => new SidecarControlExtension<TSidecar>(_sidecarState));
        */

        if (_sidecarState.State.Enabled)
        {
            var sidecar = _grainFactory.GetGrain<TSidecar>(_grain.GetGrainId());
            await sidecar.InitializeSidecar();
        }
    }

    private Task OnDeactivateAsync(CancellationToken ct)
    {
        // optional: notify sidecar shutdown
        return Task.CompletedTask;
    }
}

