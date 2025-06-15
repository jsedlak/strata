using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Strata;
using Strata.Snapshotting;

namespace Strata;

public abstract class EventSourcedGrain<TGrainState, TEventBase> : Grain, ILifecycleParticipant<IGrainLifecycle>
    where TGrainState : class, new()
    where TEventBase : class
{
    private IDisposable? _saveTimer;
    private bool _saveOnRaise = false;
    private ISnapshotStrategy<TGrainState>? _snapshotStrategy;
    private IEventLog<TGrainState, TEventBase> _eventLog = null!;
    
    public virtual void Participate(IGrainLifecycle lifecycle)
    {
        lifecycle.Subscribe<EventSourcedGrain<TGrainState, TEventBase>>(
            GrainLifecycleStage.SetupState,
            OnSetup,
            OnTearDown
        );

        lifecycle.Subscribe<EventSourcedGrain<TGrainState, TEventBase>>(
            GrainLifecycleStage.Activate - 1,
            OnHydrateState,
            OnDestroyState
        );
    }
    
    #region Interaction
    protected virtual Task Raise(TEventBase @event)
    {
        _eventLog.Submit(@event);

        return _saveOnRaise ? _eventLog.WaitForConfirmation() : Task.CompletedTask;
    }

    protected virtual Task Raise(IEnumerable<TEventBase> events)
    {
        _eventLog.Submit(events);
        
        return _saveOnRaise ? _eventLog.WaitForConfirmation() : Task.CompletedTask;
    }

    protected Task WaitForConfirmation()
    {
        return _eventLog.WaitForConfirmation();
    }

    protected Task Snapshot(bool truncate)
    {
        return _eventLog.Snapshot(truncate);
    }
    
    protected virtual async Task OnSaveTimerTicked(object arg)
    {
        // ensure all events are persisted
        await _eventLog.WaitForConfirmation();
        
        // check if we need to snapshot and truncate the log
        if (_snapshotStrategy is not null)
        {
            var snapshotResult = await _snapshotStrategy.ShouldSnapshot(ConfirmedState, ConfirmedVersion);

            if (snapshotResult.shouldSnapshot)
            {
                await _eventLog.Snapshot(snapshotResult.shouldTruncate);
            }
        }
    }
    #endregion

    #region Service Setup / TearDown
    /// <summary>
    /// Disposes of any references
    /// </summary>
    private Task OnTearDown(CancellationToken token) => Task.CompletedTask;

    /// <summary>
    /// Grabs all required services from the ServiceProvider
    /// </summary>
    private Task OnSetup(CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }
        
        // grab the event log factory and build the log service
        var factory = ServiceProvider.GetRequiredService<IEventLogFactory>();
        _eventLog = factory.Create<TGrainState, TEventBase>(GetType(), this.GetGrainId().ToString());

        // attempt to grab a snapshot strategy
        var snapshotFactory = ServiceProvider.GetService<ISnapshotStrategyFactory>();
        if (snapshotFactory != null)
        {
            _snapshotStrategy = snapshotFactory.Create<TGrainState>(GetType(), this.GetGrainId().ToString());
        }
        
        return Task.CompletedTask;
    }
    #endregion

    #region Hydrate / Destroy
    /// <summary>
    /// Responsible for hydrating the state from storage
    /// </summary>
    private async Task OnHydrateState(CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }

        await _eventLog.Hydrate();

        var timer = GetType().GetCustomAttribute<PersistTimerAttribute>()?.Time ??
                        PersistTimerAttribute.DefaultTime;

        if (timer.Equals(TimeSpan.Zero))
        {
            _saveOnRaise = true;
        }
        else {
            _saveTimer = this.RegisterGrainTimer(OnSaveTimerTicked, new object(), timer, timer);
        }
    }

    /// <summary>
    /// Called when the grain is deactivating
    /// </summary>
    private async Task OnDestroyState(CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }

        await _eventLog.WaitForConfirmation();

        if (_saveTimer is not null)
        {
            _saveTimer.Dispose();
            _saveTimer = null;
        }
    }
    #endregion

    protected TGrainState TentativeState => _eventLog.TentativeView;

    protected int TentativeVersion => _eventLog.TentativeVersion;

    protected TGrainState ConfirmedState => _eventLog.ConfirmedView;

    protected int ConfirmedVersion => _eventLog.ConfirmedVersion;
}