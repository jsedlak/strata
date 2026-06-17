using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Journaling;
using Orleans.Journaling.Json;
using Orleans.TestingHost;
using Strata.Journaling.Tests.JournalingTests.Events;
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
        var storageProvider = new VolatileJournalStorageProvider();

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
            siloBuilder.AddMemoryGrainStorageAsDefault();
            siloBuilder.UseInMemoryReminderService();
            siloBuilder.ConfigureLogging(ConfigureConsoleLogging);
            siloBuilder.AddJournalStorage();
            siloBuilder.Services.AddSingleton<IJournalStorageProvider>(storageProvider);
            siloBuilder.UseJsonJournalFormat(CreateJournalTypeResolver());

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

    /// <summary>
    /// The JSON journal format serializes events with System.Text.Json, which (unlike the Orleans
    /// serializer) needs polymorphism declared explicitly to round-trip events stored under their
    /// abstract base type. Without this, replaying a persisted <see cref="BaseAccountEvent"/> throws
    /// NotSupportedException because the concrete type information is lost.
    /// </summary>
    private static IJsonTypeInfoResolver CreateJournalTypeResolver()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(static typeInfo =>
        {
            if (typeInfo.Type == typeof(BaseAccountEvent))
            {
                typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = "$type",
                    DerivedTypes =
                    {
                        new JsonDerivedType(typeof(BalanceAdjustedEvent), nameof(BalanceAdjustedEvent)),
                    },
                };
            }
        });
        return resolver;
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
