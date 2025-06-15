using Orleans.TestingHost;
using Strata;

namespace Strata.Tests;

public class DefaultSiloConfigurator : ISiloConfigurator
{
    public virtual void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.Services.AddOrleansSerializers();
        siloBuilder.Services.AddMemoryEventSourcing();
        siloBuilder.UseInMemoryReminderService();
    }
}