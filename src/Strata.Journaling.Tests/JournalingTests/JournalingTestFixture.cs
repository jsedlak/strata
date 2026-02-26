using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Journaling;
using Orleans.TestingHost;
using Xunit;

namespace Strata.Journaling.Tests;

/// <summary>
/// Base class for journaling tests with common setup using InProcessTestCluster
/// </summary>
public class JournalingTestFixture : IAsyncLifetime
{
    public InProcessTestCluster Cluster { get; }
    public IClusterClient Client => Cluster.Client;

    public JournalingTestFixture()
    {
        var builder = new InProcessTestClusterBuilder();
        var storageProvider = new VolatileStateMachineStorageProvider();

        static void ConfigureConsoleLogging(ILoggingBuilder logging)
        {
            logging.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
            logging.SetMinimumLevel(LogLevel.Information);
        }

        builder.ConfigureSilo((options, siloBuilder) =>
        {
            siloBuilder.ConfigureLogging(ConfigureConsoleLogging);
            siloBuilder.AddStateMachineStorage();
            siloBuilder.Services.AddSingleton<IStateMachineStorageProvider>(storageProvider);

            // configure a really low idle collection age to test the outbox timer
            siloBuilder.Services.Configure<GrainCollectionOptions>(options =>
            {
                options.CollectionAge = TimeSpan.FromSeconds(61);
            });
        });

        ConfigureTestCluster(builder);
        Cluster = builder.Build();
    }

    protected virtual void ConfigureTestCluster(InProcessTestClusterBuilder builder)
    {
    }

    public virtual async Task InitializeAsync()
    {
        await Cluster.DeployAsync();
    }

    public virtual async Task DisposeAsync()
    {
        if (Cluster != null)
        {
            await Cluster.DisposeAsync();
        }
    }

    
}
