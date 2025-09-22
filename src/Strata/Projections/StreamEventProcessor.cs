using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Streams;

namespace Strata.Projections
{
    /// <summary>
    /// Processes stream events for projection grains.
    /// </summary>
    public class StreamEventProcessor
    {
        private readonly ILogger<StreamEventProcessor> _logger;
        private readonly Dictionary<Type, MethodInfo> _eventHandlers;
        private readonly object _projectionInstance;

        public StreamEventProcessor(object projectionInstance, ILogger<StreamEventProcessor> logger)
        {
            _projectionInstance = projectionInstance ?? throw new ArgumentNullException(nameof(projectionInstance));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventHandlers = new Dictionary<Type, MethodInfo>();
            
            DiscoverEventHandlers();
        }

        /// <summary>
        /// Processes a stream event by routing it to the appropriate handler method.
        /// </summary>
        /// <param name="event">The event to process.</param>
        /// <param name="token">Optional stream sequence token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ProcessEventAsync(object @event, StreamSequenceToken token = null)
        {
            if (@event == null)
            {
                _logger.LogWarning("Received null event in stream processor");
                return;
            }

            try
            {
                var eventType = @event.GetType();
                
                if (_eventHandlers.TryGetValue(eventType, out var handler))
                {
                    _logger.LogDebug("Processing stream event {EventType} with token {Token}", 
                        eventType.Name, token?.ToString() ?? "null");

                    var task = (Task)handler.Invoke(_projectionInstance, new[] { @event });
                    if (task != null)
                    {
                        await task;
                    }
                    
                    _logger.LogDebug("Successfully processed stream event {EventType}", eventType.Name);
                }
                else
                {
                    _logger.LogWarning("No handler found for stream event type {EventType}. Available handlers: {HandlerTypes}", 
                        eventType.Name, string.Join(", ", _eventHandlers.Keys.Select(t => t.Name)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing stream event {EventType} with token {Token}", 
                    @event.GetType().Name, token?.ToString() ?? "null");
                throw;
            }
        }

        /// <summary>
        /// Processes multiple stream events in batch.
        /// </summary>
        /// <param name="events">The events to process.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ProcessEventsAsync(IEnumerable<object> events)
        {
            if (events == null)
            {
                _logger.LogWarning("Received null events collection in stream processor");
                return;
            }

            var eventList = events.ToList();
            _logger.LogDebug("Processing batch of {EventCount} stream events", eventList.Count);

            var tasks = eventList.Select(@event => ProcessEventAsync(@event));
            await Task.WhenAll(tasks);

            _logger.LogDebug("Completed processing batch of {EventCount} stream events", eventList.Count);
        }

        /// <summary>
        /// Gets the event types that this processor can handle.
        /// </summary>
        /// <returns>A collection of event types.</returns>
        public IEnumerable<Type> GetSupportedEventTypes()
        {
            return _eventHandlers.Keys.ToList();
        }

        /// <summary>
        /// Checks if this processor can handle the specified event type.
        /// </summary>
        /// <param name="eventType">The event type to check.</param>
        /// <returns>True if the processor can handle the event type; otherwise, false.</returns>
        public bool CanHandleEventType(Type eventType)
        {
            return _eventHandlers.ContainsKey(eventType);
        }

        /// <summary>
        /// Discovers event handler methods in the projection instance.
        /// </summary>
        private void DiscoverEventHandlers()
        {
            var methods = _projectionInstance.GetType().GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var method in methods)
            {
                if (IsEventHandlerMethod(method))
                {
                    var eventType = method.GetParameters().FirstOrDefault()?.ParameterType;
                    if (eventType != null)
                    {
                        _eventHandlers[eventType] = method;
                        _logger.LogDebug("Discovered stream event handler for type {EventType}", eventType.Name);
                    }
                }
            }

            _logger.LogInformation("Discovered {HandlerCount} stream event handlers for projection {ProjectionType}", 
                _eventHandlers.Count, _projectionInstance.GetType().Name);
        }

        /// <summary>
        /// Determines if a method is an event handler method.
        /// </summary>
        private bool IsEventHandlerMethod(MethodInfo method)
        {
            // Check if method name is "Handle" and has exactly one parameter
            if (method.Name != "Handle" || method.GetParameters().Length != 1)
                return false;

            // Check if method returns Task or Task<T>
            if (!typeof(Task).IsAssignableFrom(method.ReturnType))
                return false;

            // Check if the class implements IProjection<T> for the event type
            var eventType = method.GetParameters()[0].ParameterType;
            var projectionInterface = typeof(IProjection<>).MakeGenericType(eventType);
            
            return projectionInterface.IsAssignableFrom(_projectionInstance.GetType());
        }
    }
}
