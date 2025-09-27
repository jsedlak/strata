using Microsoft.Extensions.DependencyInjection;
using Orleans.Journaling;

namespace Strata;

public abstract class JournaledGrainBase<TModel, TEvent> : DurableGrain, IJournaledGrain
    where TModel : IAggregate, new()
    where TEvent : notnull
{
    private readonly IDurableList<TEvent> _eventLog;
    private readonly IDurableQueue<OutboxEnvelope<TEvent>> _outbox;
    private readonly IPersistentState<TModel> _state;

    private readonly Dictionary<string, IOutboxRecipient<TEvent>> _outboxRecipients = new();

    private IGrainTimer? _outboxTimer = null;

    public JournaledGrainBase(
        [FromKeyedServices("log")] IDurableList<TEvent> eventLog,
        [FromKeyedServices("outbox")] IDurableQueue<OutboxEnvelope<TEvent>> outbox,
        [FromKeyedServices("state")] IPersistentState<TModel> state
    )
    {
        _eventLog = eventLog;
        _outbox = outbox;
        _state = state;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        
        _outboxTimer = this.RegisterGrainTimer(
            ProcessOutbox,
            new GrainTimerCreationOptions
            {
                DueTime = Timeout.InfiniteTimeSpan,
                Period = Timeout.InfiniteTimeSpan,
                Interleave = true,
            }
        );
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await this.ProcessOutbox();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    protected void RegisterRecipient(string key, IOutboxRecipient<TEvent> recipient)
    {
        _outboxRecipients.Add(key, recipient);
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
        foreach (var recipient in _outboxRecipients.Keys)
        {
            _outbox.Enqueue(new OutboxEnvelope<TEvent>(
                @event,
                _state.State.Version,
                recipient,
                OutboxState.Pending
            ));
        }

        // Save it in one shot
        await WriteStateAsync();

        // Initialize background processing of the outbox
        //_ = InitializeOutboxProcessing();
        // await this.AsReference<IJournaledGrain>().ProcessOutbox();
        _outboxTimer?.Change(TimeSpan.FromSeconds(0), Timeout.InfiniteTimeSpan);
    }

    /*private async Task InitializeOutboxProcessing()
    {
        await this.AsReference<IJournaledGrain>().ProcessOutbox();
    }*/

    public async Task ProcessOutbox()
    {
        _outboxTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        var failedItems = new List<OutboxEnvelope<TEvent>>();

        while(_outbox.TryDequeue(out var item))
        {
            try
            {
                if (_outboxRecipients.TryGetValue(item.Destination, out var recipient))
                {
                    Console.WriteLine("[{0}] Handling event {1} for recipient {2}", this.GetPrimaryKeyString(), item.Event.GetType().Name, item.Destination);

                    await recipient.Handle(item.Version, item.Event);
                    await WriteStateAsync();
                }
                else
                {
                    throw new InvalidOperationException($"No recipient registered for destination: {item.Destination}");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("[{0}] Failed to handle event {1} for recipient {2}", this.GetPrimaryKeyString(), item.Event.GetType().Name, item.Destination);
                // Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);

                item.State = OutboxState.Failed;
                failedItems.Add(item);
            }
        }

        foreach(var item in failedItems)
        {
            _outbox.Enqueue(item);
            
        }

        await WriteStateAsync();
    }

    protected TEvent[] Log => _eventLog.ToArray();

    protected TModel ConfirmedState => _state.State;
}
