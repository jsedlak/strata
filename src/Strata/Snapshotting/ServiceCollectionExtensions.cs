using Microsoft.Extensions.DependencyInjection;

namespace Strata.Snapshotting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPredicatedSnapshotting(
        this IServiceCollection serviceCollection,
        Func<Type, string, (bool, bool)> predicate)
    {
        // configure to always snapshot and never truncate
        serviceCollection.Configure<PredicatedSnapshotStrategyFactoryOptions>(options =>
            options.ShouldSnapshot = predicate);

        // configure the strategy
        serviceCollection.AddScoped<ISnapshotStrategyFactory, PredicatedSnapshotStrategyFactory>();

        return serviceCollection;
    }
}