namespace Strata;

[GenerateSerializer]
public class OutboxEnvelope<TEvent>(TEvent Event, int Version, string Destination, OutboxState State);
