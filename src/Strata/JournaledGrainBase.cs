using Microsoft.Extensions.DependencyInjection;
using Orleans.Journaling;

namespace Strata;

public abstract class JournaledGrainBase<TModel, TEvent> : DurableGrain
    where TModel : IAggregate, new()
{
    private readonly IDurableList<TEvent> _eventLog;
    private readonly IDurableQueue<OutboxEnvelope<TEvent>> _outbox;
    private readonly IPersistentState<TModel> _state;

    public JournaledGrainBase(
        [FromKeyedServices("log")]IDurableList<TEvent> eventLog,
        [FromKeyedServices("outbox")]IDurableQueue<OutboxEnvelope<TEvent>> outbox,
        [FromKeyedServices("state")]IPersistentState<TModel> state
    )
    {
        _eventLog = eventLog;
        _outbox = outbox;
        _state = state;
    }

    protected virtual async Task RaiseEvent(TEvent @event)
    {
        // add it to the log
        _eventLog.Add(@event);

        // apply it to the state
        dynamic e = @event!;
        dynamic s = _state.State;
        s.Apply(e);

        // update the version
        _state.State.Version += 1;

        // add it to the outbox ... we can loop through a list of providers and add one per provider
        // we pass the event and the version, so that the consumer can handle ordering / deduplication
        _outbox.Append(new OutboxEnvelope<TEvent>(
            @event,
            _state.State.Version,
            "OrleansStream",
            OutboxState.Pending
        ));

        // Save it in one shot
        await WriteStateAsync();

        // initiate background processing of the outbox ??
    }

    protected TEvent[] Log => _eventLog.ToArray();

    protected TModel ConfirmedState => _state.State;
}
