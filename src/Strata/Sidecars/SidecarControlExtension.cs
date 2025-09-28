namespace Strata.Sidecars;

internal sealed class SidecarControlExtension
    : ISidecarControlExtension
{
    private readonly IPersistentState<SidecarState> _state;
    public SidecarControlExtension(IPersistentState<SidecarState> state) 
        => _state = state;

    public Task EnableSidecar()
    {
        _state.State.Enabled = true;
        return _state.WriteStateAsync();
    }

    public Task DisableSidecar()
    {
        _state.State.Enabled = false;
        return _state.WriteStateAsync();
    }
}

