using Microsoft.Extensions.DependencyInjection;

namespace Strata;

public static class ServiceExtensions
{
    public static void AddMemoryEventSourcing(this IServiceCollection services)
    {
        services.AddScoped<IEventLogFactory, InMemoryEventLogFactory>();
    }

    public static void AddOrleansSerializers(this IServiceCollection services)
    {
        services.AddSingleton<IEventSerializer, OrleansEventSerializer>();
        services.AddSingleton<IStateSerializer, OrleansStateSerializer>();
    }
}