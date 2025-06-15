namespace Strata.Snapshotting;

public sealed class PredicatedSnapshotStrategyFactoryOptions
{
    public Func<Type, string, (bool shouldSnapshot, bool shouldTruncate)> ShouldSnapshot { get; set; } = null!;
}