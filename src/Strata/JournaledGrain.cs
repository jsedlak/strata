using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Journaling;

namespace Strata;

public abstract class JournaledGrain<TModel, TEvent> :
    DurableGrain, IJournaledGrain, ILifecycleParticipant<IGrainLifecycle>, IRemindable
    where TModel : new()
    where TEvent : notnull
{
    private const string OutboxCleanupReminderName = "outbox-cleanup";

    private IDurableList<EventEnvelope<TEvent>> _journal = null!;
    private IDurableQueue<OutboxEnvelope<TEvent>> _outbox = null!;
    private IPersistentState<AggregateEnvelope<TModel>> _aggregate = null!;

    private readonly Dictionary<string, IOutboxRecipient<TEvent>> _outboxRecipients = new();

    private ILogger<IJournaledGrain> _logger = null!;

    private Task? _outboxProcessingTask = null;


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
        var loggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger<IJournaledGrain>();

        _logger.LogInformation("OnHydrateState");

        _aggregate = ServiceProvider.GetRequiredKeyedService<IPersistentState<AggregateEnvelope<TModel>>>("aggregate");
        _journal = ServiceProvider.GetRequiredKeyedService<IDurableList<EventEnvelope<TEvent>>>("journal");
        _outbox = ServiceProvider.GetRequiredKeyedService<IDurableQueue<OutboxEnvelope<TEvent>>>("outbox");

        if (!_aggregate.RecordExists)
        {
            _aggregate.State = new()
            {
                Version = 1
            };

            await WriteStateAsync();
        }

        OnRegisterRecipients();
    }

    protected virtual void OnRegisterRecipients()
    {
        /* no op */
    }

    private async Task OnDestroyState(CancellationToken cancellationToken)
    {
        /* try to process the outbox */
        if(_outbox.Count < 0)
        {
            _logger.LogInformation("Registering reminder for outbox processing.");
            await this.RegisterOrUpdateReminder(
                OutboxCleanupReminderName,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1)
            );
        }
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if(reminderName == OutboxCleanupReminderName)
        {
            _logger.LogInformation("Received reminder for outbox cleanup.");
            
            var reminder = await this.GetReminder(OutboxCleanupReminderName);
            if(reminder is not null)
            {
                await this.UnregisterReminder(reminder);
            }

            await ProcessOutboxInBackground();
        }
    }
    #endregion

    #region Recipient Management
    protected void RegisterRecipient(string key, IOutboxRecipient<TEvent> recipient)
    {
        _outboxRecipients.Add(key, recipient);
    }
    #endregion

    #region Event Processing
    protected async Task TryProcessOutbox()
    {
        Task? processingTask = ProcessOutbox();
        if (processingTask is not null)
        {
            await processingTask;
        }
    }
    
    protected Task? ProcessOutbox() 
    {
        if(_outboxProcessingTask is not { IsCompleted: true })
        {
            _logger.LogInformation("Starting outbox processing task.");
            _outboxProcessingTask = Task.Factory.StartNew(ProcessOutboxInBackground);
        }
        else
        {
            _logger.LogInformation("Outbox is empty, no processing needed.");
        }

        return _outboxProcessingTask;
    }

    private async Task ProcessOutboxInBackground()
    {
        // It's polite to yield immediately, since we're starting background work.
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding | ConfigureAwaitOptions.ContinueOnCapturedContext);

        // store the failed items in a list to requeue
        var failedItems = new List<OutboxEnvelope<TEvent>>();

        while (_outbox.TryDequeue(out var item))
        {
            try
            {
                if (_outboxRecipients.TryGetValue(item.Destination, out var recipient))
                {
                    _logger.LogInformation("[{0}] Handling event {1} for recipient {2}", this.GetPrimaryKeyString(), item.Event.GetType().Name, item.Destination);

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
                _logger.LogError(ex, "[{0}] Failed to handle event {1} for recipient {2}", this.GetPrimaryKeyString(), item.Event.GetType().Name, item.Destination);

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

    protected virtual async Task RaiseEvent(TEvent @event)
    {
        var newVersion = _aggregate.State.Version + 1;

        // add it to the log
        _journal.Add(new EventEnvelope<TEvent> { 
            Event = @event, 
            Version = newVersion,
            Timestamp = DateTimeOffset.UtcNow
        });

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

        await TryProcessOutbox();
    }
    #endregion

    protected EventEnvelope<TEvent>[] Log => _journal.ToArray();

    protected TModel ConfirmedState => _aggregate.State.Aggregate;

    protected int ConfirmedVersion => _aggregate.State.Version;

}
