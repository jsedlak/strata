namespace Strata;

[GenerateSerializer]
public class OutboxEnvelope<TEvent>
{
    public OutboxEnvelope()
    {
    }

    public OutboxEnvelope(TEvent? @event, int version, string destination, OutboxState state)
    {
        Event = @event;
        Version = version;
        Destination = destination;
        State = state;
    }

    [Id(0)]
    public TEvent Event { get; set; }

    [Id(1)]
    public int Version { get; set; }

    [Id(2)]
    public string Destination { get; set; } = null!;

    [Id(3)]
    public OutboxState State { get; set; }
}
