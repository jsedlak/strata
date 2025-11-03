namespace Strata;

[GenerateSerializer]
[Alias("Strata.JournalState")]
public sealed class JournalState<TModel, TEvent>
    where TModel : new()
    where TEvent : notnull
{
    [Id(0)]
    public TModel Aggregate { get; set; } = new();

    [Id(1)]
    public EventEnvelope<TEvent>[] Events { get; set; } = Array.Empty<EventEnvelope<TEvent>>();

    [Id(2)]
    public OutboxEnvelope<TEvent>[] Outbox { get; set; } = Array.Empty<OutboxEnvelope<TEvent>>();

    [Id(3)]
    public int Version { get; set; }
}