using Microsoft.Extensions.DependencyInjection;
using Orleans.Journaling;

namespace Strata;

public abstract class JournaledGrain<TModel, TEvent> :
    Grain, IJournaledGrain, ILifecycleParticipant<IGrainLifecycle>
    where TModel : new()
    where TEvent : notnull
{
    // private IDurableList<EventEnvelope<TEvent>> _journal = null!;
    // private IDurableQueue<OutboxEnvelope<TEvent>> _outbox = null!;
    // private IPersistentState<AggregateEnvelope<TModel>> _aggregate = null!;
    private IPersistentState<JournalState<TModel, TEvent>> _journal = null!;

    private readonly Dictionary<string, IOutboxRecipient<TEvent>> _outboxRecipients = new();

    private IGrainTimer? _outboxTimer = null;

    protected JournaledGrain(
        [PersistentState("journal")] IPersistentState<JournalState<TModel, TEvent>> journal
    )
    {
        _journal = journal;
    }

    #region Lifecycle
    public void Participate(IGrainLifecycle lifecycle)
    {
        lifecycle.Subscribe<JournaledGrain<TModel, TEvent>>(
            GrainLifecycleStage.Activate - 1,
            OnHydrateState,
            OnDestroyState
        );
    }

    private async Task OnHydrateState(CancellationToken cancellationToken)
    {
        Console.WriteLine("OnHydrateState");

        //_journal = ServiceProvider.GetRequiredKeyedService<>("journalState");

        if(!_journal.RecordExists)
        {
            _journal.State = new JournalState<TModel, TEvent>();

            await _journal.WriteStateAsync();
        }

        // _aggregate = ServiceProvider.GetRequiredKeyedService<IPersistentState<AggregateEnvelope<TModel>>>("aggregate");
        // _journal = ServiceProvider.GetRequiredKeyedService<IDurableList<EventEnvelope<TEvent>>>("journal");
        // _outbox = ServiceProvider.GetRequiredKeyedService<IDurableQueue<OutboxEnvelope<TEvent>>>("outbox");

        //if (!_aggregate.RecordExists)
        //{
        //    _aggregate.State = new();
        //    _aggregate.State.Version = 1;

        //    await WriteStateAsync();
        //}

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
        var newVersion = _journal.State.Version + 1;

        // add it to the log
        _journal.State.Events = _journal.State.Events.Union([
            new EventEnvelope<TEvent> { Event = @event, Version = newVersion, Timestamp = DateTime.UtcNow }
        ]).ToArray();

        // apply it to the state
        dynamic e = @event!;
        dynamic s = _journal.State.Aggregate!;
        s.Apply(e);

        // update the version
        _journal.State.Version = newVersion;

        // add it to the outbox ... we can loop through a list of providers and add one per provider
        // we pass the event and the version, so that the consumer can handle ordering / deduplication
        foreach (var recipient in _outboxRecipients.Keys)
        {
            _journal.State.Outbox = _journal.State.Outbox.Union([
                new OutboxEnvelope<TEvent>(
                    @event,
                    newVersion,
                    recipient,
                    OutboxState.Pending
                )
            ]).ToArray();
            //_outbox.Enqueue(new OutboxEnvelope<TEvent>(
            //    @event,
            //    newVersion,
            //    recipient,
            //    OutboxState.Pending
            //));
        }

        // Save it in one shot
        await _journal.WriteStateAsync();

        // Initialize background processing of the outbox
        _outboxTimer?.Change(TimeSpan.FromSeconds(0), Timeout.InfiniteTimeSpan);
    }

    public async Task ProcessOutbox()
    {
        _outboxTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        // var failedItems = new List<OutboxEnvelope<TEvent>>();

        var pendingItems = _journal.State.Outbox
            .Where(o => o.State == OutboxState.Pending)
            .ToList();

        foreach(var item in pendingItems)
        {
            try
            {
                if (_outboxRecipients.TryGetValue(item.Destination, out var recipient))
                {
                    Console.WriteLine("[{0}] Handling event {1} for recipient {2}", this.GetPrimaryKeyString(), item.Event.GetType().Name, item.Destination);

                    await recipient.Handle(item.Version, item.Event);
                    await _journal.WriteStateAsync();
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
                //failedItems.Add(item);
            }
        }

        //foreach (var item in failedItems)
        //{
        //    _outbox.Enqueue(item);

        //}

        await _journal.WriteStateAsync();
    }
    #endregion

    protected EventEnvelope<TEvent>[] Log => _journal.State.Events;

    protected TModel ConfirmedState => _journal.State.Aggregate;

    protected int ConfirmedVersion => _journal.State.Version;

}
