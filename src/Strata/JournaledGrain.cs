using Microsoft.Extensions.DependencyInjection;
using Orleans.Journaling;

namespace Strata;

public abstract class JournaledGrain<TModel, TEvent> :
    DurableGrain, IJournaledGrain, ILifecycleParticipant<IGrainLifecycle>
    where TModel : new()
    where TEvent : notnull
{
    private IDurableList<EventEnvelope<TEvent>> _journal = null!;
    private IDurableQueue<OutboxEnvelope<TEvent>> _outbox = null!;
    private IPersistentState<AggregateEnvelope<TModel>> _aggregate = null!;

    private readonly Dictionary<string, IOutboxRecipient<TEvent>> _outboxRecipients = new();

    private IGrainTimer? _outboxTimer = null;

    #region Lifecycle
    public void Participate(IGrainLifecycle lifecycle)
    {
        lifecycle.Subscribe<JournaledGrain<TModel, TEvent>>(
            GrainLifecycleStage.SetupState - 1,
            OnHydrateState,
            OnDestroyState
        );
    }

    private async Task OnHydrateState(CancellationToken cancellationToken)
    {
        Console.WriteLine("OnHydrateState");

        _aggregate = ServiceProvider.GetRequiredKeyedService<IPersistentState<AggregateEnvelope<TModel>>>("aggregate");
        _journal = ServiceProvider.GetRequiredKeyedService<IDurableList<EventEnvelope<TEvent>>>("journal");
        _outbox = ServiceProvider.GetRequiredKeyedService<IDurableQueue<OutboxEnvelope<TEvent>>>("outbox");

        if (!_aggregate.RecordExists)
        {
            _aggregate.State = new();
            _aggregate.State.Version = 1;

            await WriteStateAsync();
        }

        OnRegisterRecipients();

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

    protected virtual void OnRegisterRecipients()
    {
        /* no op */
    }

    private async Task OnDestroyState(CancellationToken cancellationToken)
    {
        /* try to process the outbox */
        await ProcessOutbox();
    }
    #endregion

    #region Recipient Management
    protected void RegisterRecipient(string key, IOutboxRecipient<TEvent> recipient)
    {
        _outboxRecipients.Add(key, recipient);
    }
    #endregion

    #region Event Processing
    protected virtual async Task RaiseEvent(TEvent @event)
    {
        var newVersion = _aggregate.State.Version + 1;

        // add it to the log
        _journal.Add(new EventEnvelope<TEvent> { Event = @event, Version = newVersion });

        // apply it to the state
        dynamic e = @event!;
        dynamic s = _aggregate.State.Aggregate!;
        s.Apply(e);

        // update the version
        _aggregate.State.Version = newVersion;

        // add it to the outbox ... we can loop through a list of providers and add one per provider
        // we pass the event and the version, so that the consumer can handle ordering / deduplication
        foreach (var recipient in _outboxRecipients.Keys)
        {
            _outbox.Enqueue(new OutboxEnvelope<TEvent>(
                @event,
                newVersion,
                recipient,
                OutboxState.Pending
            ));
        }

        // Save it in one shot
        await WriteStateAsync();

        // Initialize background processing of the outbox
        _outboxTimer?.Change(TimeSpan.FromSeconds(0), Timeout.InfiniteTimeSpan);
    }

    public async Task ProcessOutbox()
    {
        _outboxTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        var failedItems = new List<OutboxEnvelope<TEvent>>();

        while (_outbox.TryDequeue(out var item))
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
            catch (Exception ex)
            {
                Console.WriteLine("[{0}] Failed to handle event {1} for recipient {2}", this.GetPrimaryKeyString(), item.Event.GetType().Name, item.Destination);
                // Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);

                item.State = OutboxState.Failed;
                failedItems.Add(item);
            }
        }

        foreach (var item in failedItems)
        {
            _outbox.Enqueue(item);

        }

        await WriteStateAsync();
    }
    #endregion

    protected EventEnvelope<TEvent>[] Log => _journal.ToArray();

    protected TModel ConfirmedState => _aggregate.State.Aggregate;

    protected int ConfirmedVersion => _aggregate.State.Version;

}
