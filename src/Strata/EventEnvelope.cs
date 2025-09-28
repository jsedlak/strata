namespace Strata;

[GenerateSerializer]
[Alias("Strata.EventEnvelope")]
public class EventEnvelope<TEvent>
    where TEvent : notnull
{
    [Id(0)]
    public TEvent Event { get; set; } = default!;

    [Id(1)]
    public int Version { get; set; }
}