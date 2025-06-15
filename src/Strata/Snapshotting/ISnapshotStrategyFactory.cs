namespace Strata.Snapshotting;

public interface ISnapshotStrategyFactory
{
    ISnapshotStrategy<TView> Create<TView>(Type grainType, string viewId)
        where TView : class, new();
}