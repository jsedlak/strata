using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;

namespace Strata.Projections
{
    /// <summary>
    /// A grain that processes projection events asynchronously.
    /// </summary>
    public class ProjectionGrain : Grain, IProjectionGrain
    {
        private readonly ILogger<ProjectionGrain> _logger;
        private readonly ProjectionOptions _options;
        private readonly ConcurrentQueue<ProjectionWorkItem> _workQueue;
        private readonly SemaphoreSlim _semaphore;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Dictionary<string, Type> _projectionTypes;
        private readonly Dictionary<string, object> _projectionInstances;
        private Task _processingTask;
        private bool _isProcessing;

        public ProjectionGrain(ILogger<ProjectionGrain> logger, IOptions<ProjectionOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _workQueue = new ConcurrentQueue<ProjectionWorkItem>();
            _semaphore = new SemaphoreSlim(_options.MaxConcurrency, _options.MaxConcurrency);
            _cancellationTokenSource = new CancellationTokenSource();
            _projectionTypes = new Dictionary<string, Type>();
            _projectionInstances = new Dictionary<string, object>();
        }

        public override Task OnActivateAsync()
        {
            _logger.LogInformation("ProjectionGrain {GrainId} activated", this.GetPrimaryKeyString());
            _isProcessing = true;
            _processingTask = ProcessProjectionsAsync();
            return base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            _logger.LogInformation("ProjectionGrain {GrainId} deactivating", this.GetPrimaryKeyString());
            _isProcessing = false;
            _cancellationTokenSource.Cancel();
            return base.OnDeactivateAsync();
        }

        public Task ApplyProjection<TEvent>(TEvent @event, string projectionType) where TEvent : class
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));
            if (string.IsNullOrEmpty(projectionType))
                throw new ArgumentNullException(nameof(projectionType));

            var workItem = new ProjectionWorkItem
            {
                Event = @event,
                EventType = typeof(TEvent),
                ProjectionType = projectionType,
                Timestamp = DateTime.UtcNow
            };

            if (_workQueue.Count >= _options.MaxQueueSize)
            {
                _logger.LogWarning("Projection queue is full, dropping event {EventType} for projection {ProjectionType}", 
                    typeof(TEvent).Name, projectionType);
                return Task.CompletedTask;
            }

            _workQueue.Enqueue(workItem);
            _logger.LogDebug("Queued projection event {EventType} for projection {ProjectionType}", 
                typeof(TEvent).Name, projectionType);

            return Task.CompletedTask;
        }

        private async Task ProcessProjectionsAsync()
        {
            while (_isProcessing && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var batch = new List<ProjectionWorkItem>();
                    
                    // Collect a batch of work items
                    for (int i = 0; i < _options.BatchSize && _workQueue.TryDequeue(out var workItem); i++)
                    {
                        batch.Add(workItem);
                    }

                    if (batch.Count > 0)
                    {
                        // Process the batch concurrently
                        var tasks = batch.Select(ProcessProjectionAsync);
                        await Task.WhenAll(tasks);
                    }
                    else
                    {
                        // No work to do, wait a bit
                        await Task.Delay(100, _cancellationTokenSource.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when shutting down
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in projection processing loop");
                    await Task.Delay(1000, _cancellationTokenSource.Token);
                }
            }
        }

        private async Task ProcessProjectionAsync(ProjectionWorkItem workItem)
        {
            await _semaphore.WaitAsync(_cancellationTokenSource.Token);
            try
            {
                await ProcessProjectionInternalAsync(workItem);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task ProcessProjectionInternalAsync(ProjectionWorkItem workItem)
        {
            var retryCount = 0;
            Exception lastException = null;

            while (retryCount <= _options.MaxRetryAttempts)
            {
                try
                {
                    var projection = GetOrCreateProjection(workItem.ProjectionType, workItem.EventType);
                    if (projection == null)
                    {
                        _logger.LogWarning("Could not create projection instance for type {ProjectionType}", 
                            workItem.ProjectionType);
                        return;
                    }

                    // Use reflection to call the Handle method
                    var handleMethod = GetHandleMethod(projection.GetType(), workItem.EventType);
                    if (handleMethod == null)
                    {
                        _logger.LogWarning("Could not find Handle method for projection {ProjectionType} and event {EventType}", 
                            workItem.ProjectionType, workItem.EventType.Name);
                        return;
                    }

                    var task = (Task)handleMethod.Invoke(projection, new[] { workItem.Event });
                    if (task != null)
                    {
                        await task.WaitAsync(TimeSpan.FromMilliseconds(_options.ProcessingTimeoutMs), 
                            _cancellationTokenSource.Token);
                    }

                    _logger.LogDebug("Successfully processed projection {ProjectionType} for event {EventType}", 
                        workItem.ProjectionType, workItem.EventType.Name);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    retryCount++;
                    
                    if (retryCount <= _options.MaxRetryAttempts)
                    {
                        _logger.LogWarning(ex, "Projection processing failed (attempt {RetryCount}/{MaxRetries}), retrying in {DelayMs}ms", 
                            retryCount, _options.MaxRetryAttempts, _options.RetryDelayMs);
                        await Task.Delay(_options.RetryDelayMs, _cancellationTokenSource.Token);
                    }
                }
            }

            _logger.LogError(lastException, "Projection processing failed after {MaxRetries} attempts for projection {ProjectionType} and event {EventType}", 
                _options.MaxRetryAttempts, workItem.ProjectionType, workItem.EventType.Name);

            if (_options.EnableDeadLetterQueue)
            {
                // TODO: Implement dead letter queue
                _logger.LogWarning("Dead letter queue not implemented, dropping failed projection");
            }
        }

        private object GetOrCreateProjection(string projectionType, Type eventType)
        {
            if (_projectionInstances.TryGetValue(projectionType, out var existingInstance))
            {
                return existingInstance;
            }

            var projectionTypeInfo = GetProjectionType(projectionType);
            if (projectionTypeInfo == null)
            {
                return null;
            }

            try
            {
                var instance = Activator.CreateInstance(projectionTypeInfo);
                _projectionInstances[projectionType] = instance;
                return instance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create projection instance for type {ProjectionType}", projectionType);
                return null;
            }
        }

        private Type GetProjectionType(string projectionType)
        {
            if (_projectionTypes.TryGetValue(projectionType, out var cachedType))
            {
                return cachedType;
            }

            var type = Type.GetType(projectionType);
            if (type != null)
            {
                _projectionTypes[projectionType] = type;
            }

            return type;
        }

        private MethodInfo GetHandleMethod(Type projectionType, Type eventType)
        {
            var interfaceType = typeof(IProjection<>).MakeGenericType(eventType);
            if (interfaceType.IsAssignableFrom(projectionType))
            {
                return interfaceType.GetMethod("Handle");
            }

            return null;
        }

        private class ProjectionWorkItem
        {
            public object Event { get; set; }
            public Type EventType { get; set; }
            public string ProjectionType { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
