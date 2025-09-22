using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Strata;
using Strata.Eventing;
using Strata.Projections;
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
    private EventHandlerRegistry _eventHandlerRegistry = null!;
    private EventHandlerOptions _eventHandlerOptions = new();
    private ILogger? _logger;
    private ProjectionRegistry? _projectionRegistry;
    
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
    protected virtual async Task Raise(TEventBase @event)
    {
        _logger?.LogDebug("Raising event of type {EventType}", @event.GetType().Name);
        await ProcessEventHandlers(@event);
        _logger?.LogDebug("Event handlers processed for {EventType}", @event.GetType().Name);
        
        _eventLog.Submit(@event);

        // Process projections asynchronously (fire-and-forget)
        _ = Task.Run(async () => await ProcessProjectionsAsync(@event));

        if (_saveOnRaise)
        {
            await _eventLog.WaitForConfirmation();
        }
    }

    protected virtual async Task Raise(IEnumerable<TEventBase> events)
    {
        foreach (var @event in events)
        {
            await ProcessEventHandlers(@event);
        }
        
        _eventLog.Submit(events);

        // Process projections asynchronously for each event (fire-and-forget)
        foreach (var @event in events)
        {
            _ = Task.Run(async () => await ProcessProjectionsAsync(@event));
        }

        if (_saveOnRaise)
        {
            await _eventLog.WaitForConfirmation();
        }
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

        // initialize event handler registry
        _eventHandlerRegistry = new EventHandlerRegistry();
        
        // get logger and event handler options
        _logger = ServiceProvider.GetService<ILogger<EventSourcedGrain<TGrainState, TEventBase>>>();
        _eventHandlerOptions = ServiceProvider.GetService<EventHandlerOptions>() ?? new EventHandlerOptions();
        
        // get projection registry
        _projectionRegistry = ServiceProvider.GetService<ProjectionRegistry>();
        
        // Call virtual method to allow derived classes to register handlers early
        OnSetupEventHandlers();
        
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

        // Clear event handlers on deactivation
        _eventHandlerRegistry?.Clear();
    }
    #endregion

    protected TGrainState TentativeState => _eventLog.TentativeView;

    protected int TentativeVersion => _eventLog.TentativeVersion;

    protected TGrainState ConfirmedState => _eventLog.ConfirmedView;

    protected int ConfirmedVersion => _eventLog.ConfirmedVersion;

    #region Event Handler Setup

    /// <summary>
    /// Override this method to register event handlers during grain setup.
    /// This is called after the event handler registry is initialized but before the grain is activated.
    /// </summary>
    protected virtual void OnSetupEventHandlers()
    {
        _logger?.LogDebug("OnSetupEventHandlers called for grain type {GrainType}", GetType().Name);
        // Override in derived classes to register event handlers
    }

    #endregion

    #region Event Handler Registration

    /// <summary>
    /// Registers a typed event handler for a specific event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to handle.</typeparam>
    /// <param name="handler">The handler delegate.</param>
    /// <exception cref="ArgumentNullException">Thrown when handler is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the grain is not properly initialized.</exception>
    public virtual void RegisterEventHandler<TEvent>(EventHandlerDelegate<TEvent> handler)
        where TEvent : TEventBase
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        if (_eventHandlerRegistry == null)
            throw new InvalidOperationException("Event handler registry is not initialized. Ensure the grain is properly activated.");

        _eventHandlerRegistry.RegisterEventHandler(handler);
    }

    /// <summary>
    /// Registers an untyped event handler that will be called for all events.
    /// </summary>
    /// <param name="handler">The handler delegate.</param>
    /// <exception cref="ArgumentNullException">Thrown when handler is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the grain is not properly initialized.</exception>
    public virtual void RegisterEventHandler(EventHandlerDelegate handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        if (_eventHandlerRegistry == null)
            throw new InvalidOperationException("Event handler registry is not initialized. Ensure the grain is properly activated.");

        _eventHandlerRegistry.RegisterEventHandler(handler);
    }

    #endregion

    #region Event Handler Processing

    /// <summary>
    /// Processes all registered event handlers for the given event.
    /// </summary>
    /// <param name="event">The event to process.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual async Task ProcessEventHandlers(TEventBase @event)
    {
        if (_eventHandlerRegistry == null)
        {
            _logger?.LogWarning("Event handler registry is null for event type {EventType}", @event.GetType().Name);
            return;
        }

        if (_eventHandlerRegistry.HandlerCount == 0)
        {
            _logger?.LogDebug("No event handlers registered for event type {EventType}", @event.GetType().Name);
            return;
        }

        var handlers = _eventHandlerRegistry.GetHandlersForEvent(@event.GetType());
        var handlerList = handlers.ToList();
        _logger?.LogDebug("Found {HandlerCount} handlers for event type {EventType}", handlerList.Count, @event.GetType().Name);
        
        if (handlerList.Count == 0)
        {
            _logger?.LogWarning("No handlers found for event type {EventType}. Available handler types: {HandlerTypes}", 
                @event.GetType().Name, 
                string.Join(", ", _eventHandlerRegistry.GetType().GetField("_typedHandlers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_eventHandlerRegistry)?.ToString() ?? "unknown"));
        }
        
        foreach (var handler in handlers)
        {
            try
            {
                if (_eventHandlerOptions.LogHandlerExecution)
                {
                    _logger?.LogDebug("Executing event handler for event type {EventType}", @event.GetType().Name);
                }

                var handlerTask = handler(@event);
                
                if (_eventHandlerOptions.MaxHandlerExecutionTime.HasValue)
                {
                    using var cts = new CancellationTokenSource(_eventHandlerOptions.MaxHandlerExecutionTime.Value);
                    await handlerTask.WaitAsync(cts.Token);
                }
                else
                {
                    await handlerTask;
                }

                if (_eventHandlerOptions.LogHandlerExecution)
                {
                    _logger?.LogDebug("Event handler completed successfully for event type {EventType}", @event.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                if (_eventHandlerOptions.LogHandlerErrors)
                {
                    _logger?.LogError(ex, "Event handler failed for event type {EventType}", @event.GetType().Name);
                }

                if (_eventHandlerOptions.FailFastOnHandlerError)
                {
                    throw;
                }
            }
        }
    }

    #endregion

    #region Projection Processing

    /// <summary>
    /// Processes projections for the given event asynchronously.
    /// </summary>
    /// <param name="event">The event to process projections for.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual async Task ProcessProjectionsAsync(TEventBase @event)
    {
        if (_projectionRegistry == null)
        {
            _logger?.LogDebug("Projection registry is not available, skipping projection processing for event {EventType}", @event.GetType().Name);
            return;
        }

        try
        {
            var eventType = @event.GetType();
            var projectionTypes = _projectionRegistry.GetProjectionsForEventType(eventType);

            if (!projectionTypes.Any())
            {
                _logger?.LogDebug("No projections registered for event type {EventType}", eventType.Name);
                return;
            }

            _logger?.LogDebug("Processing {ProjectionCount} projections for event type {EventType}", projectionTypes.Count, eventType.Name);

            // Process each projection asynchronously
            var projectionTasks = projectionTypes.Select(projectionType => ProcessProjectionAsync(@event, projectionType));
            await Task.WhenAll(projectionTasks);

            _logger?.LogDebug("Completed processing projections for event type {EventType}", eventType.Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing projections for event type {EventType}", @event.GetType().Name);
            // Don't rethrow - projections should not affect main event processing
        }
    }

    /// <summary>
    /// Processes a single projection for the given event.
    /// </summary>
    /// <param name="event">The event to process.</param>
    /// <param name="projectionType">The projection type to process.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ProcessProjectionAsync(TEventBase @event, Type projectionType)
    {
        try
        {
            // Get the projection grain
            var projectionGrain = GrainFactory.GetGrain<IProjectionGrain>(
                $"{this.GetGrainId()}_{projectionType.Name}");

            // Apply the projection
            await projectionGrain.ApplyProjection(@event, projectionType.FullName ?? projectionType.Name);

            _logger?.LogDebug("Successfully processed projection {ProjectionType} for event {EventType}", 
                projectionType.Name, @event.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing projection {ProjectionType} for event {EventType}", 
                projectionType.Name, @event.GetType().Name);
            // Don't rethrow - individual projection failures should not affect others
        }
    }

    #endregion
}