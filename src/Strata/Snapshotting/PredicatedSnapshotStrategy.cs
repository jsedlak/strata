namespace Strata.Snapshotting;

public sealed class PredicatedSnapshotStrategy<TView> :
    ISnapshotStrategy<TView> where TView : class, new()
{
    private readonly PredicatedSnapshotStrategyOptions _options;

    public PredicatedSnapshotStrategy(PredicatedSnapshotStrategyOptions options)
    {
        _options = options;
    }

    public Task<(bool shouldSnapshot, bool shouldTruncate)> ShouldSnapshot(TView currentState, int version)
    {
        throw new NotImplementedException();
    }
}