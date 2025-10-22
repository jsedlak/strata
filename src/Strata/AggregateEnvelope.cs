namespace Strata;

[GenerateSerializer]
[Alias("Strata.AggregateEnvelope")]
public class AggregateEnvelope<TAggregate>
    where TAggregate : new()
{
    [Id(0)]
    public TAggregate Aggregate { get; set; } = new();

    [Id(1)]
    public int Version { get; set; }
}
