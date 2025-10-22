//using Microsoft.Extensions.DependencyInjection;
//using Orleans.Journaling;
//using Orleans.TestingHost;
//using Strata.Sidecars;

//namespace Strata.Journaling.Tests;

///// <summary>
///// Base class for journaling tests with common setup using InProcessTestCluster
///// </summary>
//public class SidecarTestFixture : IAsyncLifetime
//{
//    public InProcessTestCluster Cluster { get; }
//    public IClusterClient Client => Cluster.Client;

//    public SidecarTestFixture()
//    {
//        var builder = new InProcessTestClusterBuilder();
//        var storageProvider = new VolatileStateMachineStorageProvider();

//        builder.ConfigureSilo((options, siloBuilder) =>
//        {
//            siloBuilder.AddSidecars();
//            siloBuilder.AddStateMachineStorage();
//            siloBuilder.Services.AddSingleton<IStateMachineStorageProvider>(storageProvider);
//        });
//        ConfigureTestCluster(builder);
//        Cluster = builder.Build();
//    }

//    protected virtual void ConfigureTestCluster(InProcessTestClusterBuilder builder)
//    {
//    }

//    public virtual async Task InitializeAsync()
//    {
//        await Cluster.DeployAsync();
//    }

//    public virtual async Task DisposeAsync()
//    {
//        if (Cluster != null)
//        {
//            await Cluster.DisposeAsync();
//        }
//    }

    
//}
