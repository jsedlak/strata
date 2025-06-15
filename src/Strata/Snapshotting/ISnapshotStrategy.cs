namespace Strata.Snapshotting;

public interface ISnapshotStrategy<TView> 
    where TView : class, new()
{
    Task<(bool shouldSnapshot, bool shouldTruncate)> ShouldSnapshot(TView currentState, int version);
}