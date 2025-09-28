namespace Strata.Sidecars;

[GenerateSerializer]
public sealed class SidecarState
{
    [Id(0)]
    public bool Enabled { get; set; } = true;
}

