using Orleans.TestingHost;

namespace Strata.Tests;

public abstract class OrleansTestBase<TConfigurator>
    where TConfigurator : ISiloConfigurator, new()
{
    private static TestCluster? _cluster;

    static OrleansTestBase()
    {
        _cluster = new TestClusterBuilder()
            .AddSiloBuilderConfigurator<TConfigurator>()
            .Build();

        _cluster.Deploy();
    }

    protected OrleansTestBase()
    {

    }

    //public void Dispose()
    //{
    //    _cluster.StopAllSilos();
    //}

    protected IGrainFactory Grains => _cluster.GrainFactory;

    protected IServiceProvider Services => _cluster.ServiceProvider;
}
