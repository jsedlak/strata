using Microsoft.Extensions.Options;

namespace Strata.Snapshotting;

public sealed class PredicatedSnapshotStrategyFactory : ISnapshotStrategyFactory
{
    private readonly PredicatedSnapshotStrategyFactoryOptions _options;

    public PredicatedSnapshotStrategyFactory(IOptions<PredicatedSnapshotStrategyFactoryOptions> options)
    {
        _options = options.Value;
    }

    public ISnapshotStrategy<TView> Create<TView>(Type grainType, string viewId) where TView : class, new()
    {
        return new PredicatedSnapshotStrategy<TView>(new PredicatedSnapshotStrategyOptions
        {
            ShouldSnapshot = _options.ShouldSnapshot
        });
    }
}