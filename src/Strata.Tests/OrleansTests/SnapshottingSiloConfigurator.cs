using Strata.Snapshotting;

namespace Strata.Tests;

public class SnapshottingSiloConfigurator : DefaultSiloConfigurator
{
    public override void Configure(ISiloBuilder siloBuilder)
    {
        base.Configure(siloBuilder);

        siloBuilder.Services.AddPredicatedSnapshotting((type, grainId) => (true, false));
    }
}