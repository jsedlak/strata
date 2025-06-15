using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;

namespace Strata;

public sealed class InMemoryEventLogFactory : IEventLogFactory
{
    private static readonly Regex SanitizeExpression = new Regex("[^a-zA-Z0-9 -]");
    
    private readonly IServiceProvider _serviceProvider;

    public InMemoryEventLogFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public IEventLog<TView, TEntry> Create<TView, TEntry>(Type grainType, string viewId) where TView : class, new() where TEntry : class
    {
        var stateSerializer = _serviceProvider.GetRequiredService<IStateSerializer>();
        
        // grab the name of the grain, falling back to the type name
        var grainName = viewId.IndexOf("/", StringComparison.OrdinalIgnoreCase) > 0
            ? viewId.Split(["/"], StringSplitOptions.RemoveEmptyEntries).First().Trim() :
            SanitizeExpression.Replace(grainType.Name, "");

        // grab the identifier. for orleans grains this will be the second part
        var grainId = viewId.IndexOf("/", StringComparison.OrdinalIgnoreCase) > 0
            ? viewId.Substring(viewId.IndexOf("/", StringComparison.OrdinalIgnoreCase) + 1) :
            viewId;

        var settings = new InMemoryEventLogOptions
        {
            GrainType = grainName,
            GrainId = grainId
        };
        
        return new InMemoryEventLog<TView, TEntry>(settings, stateSerializer);
    }
}