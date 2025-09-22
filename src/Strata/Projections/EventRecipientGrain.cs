using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Streams;

namespace Strata.Projections
{
    /// <summary>
    /// Base class for grains that process events via Orleans streams.
    /// </summary>
    public abstract class EventRecipientGrain : Grain
    {
        private readonly ILogger _logger;
        private readonly Dictionary<Type, MethodInfo> _eventHandlers;
        private IStreamProvider _streamProvider;
        private StreamEventProcessor _streamEventProcessor;

        protected EventRecipientGrain(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventHandlers = new Dictionary<Type, MethodInfo>();
        }

        public override async Task OnActivateAsync()
        {
            _logger.LogInformation("EventRecipientGrain {GrainId} activated", this.GetPrimaryKeyString());
            
            // Get the stream provider
            _streamProvider = GetStreamProvider("Default");
            
            // Initialize stream event processor
            _streamEventProcessor = new StreamEventProcessor(this, _logger);
            
            // Discover and register event handlers
            DiscoverEventHandlers();
            
            // Subscribe to streams
            await SubscribeToStreamsAsync();
            
            await base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            _logger.LogInformation("EventRecipientGrain {GrainId} deactivating", this.GetPrimaryKeyString());
            return base.OnDeactivateAsync();
        }

        /// <summary>
        /// Subscribes to the appropriate streams based on the grain's stream subscription attributes.
        /// </summary>
        protected virtual async Task SubscribeToStreamsAsync()
        {
            var streamSubscriptionAttributes = GetType().GetCustomAttributes<ImplicitStreamSubscriptionAttribute>();
            
            foreach (var attribute in streamSubscriptionAttributes)
            {
                var streamId = StreamId.Create(attribute.StreamNamespace, this.GetPrimaryKeyString());
                var stream = _streamProvider.GetStream<object>(streamId);
                
                await stream.SubscribeAsync(OnNextAsync, OnErrorAsync, OnCompletedAsync);
                
                _logger.LogInformation("Subscribed to stream {StreamNamespace} with ID {StreamId}", 
                    attribute.StreamNamespace, streamId);
            }
        }

        /// <summary>
        /// Handles incoming stream events.
        /// </summary>
        protected virtual async Task OnNextAsync(object item, StreamSequenceToken token = null)
        {
            try
            {
                await _streamEventProcessor.ProcessEventAsync(item, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing stream event {EventType}", item?.GetType().Name);
                throw;
            }
        }

        /// <summary>
        /// Handles stream errors.
        /// </summary>
        protected virtual Task OnErrorAsync(Exception ex)
        {
            _logger.LogError(ex, "Stream error occurred in EventRecipientGrain {GrainId}", this.GetPrimaryKeyString());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles stream completion.
        /// </summary>
        protected virtual Task OnCompletedAsync()
        {
            _logger.LogInformation("Stream completed for EventRecipientGrain {GrainId}", this.GetPrimaryKeyString());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Discovers event handler methods in the derived class.
        /// </summary>
        private void DiscoverEventHandlers()
        {
            var methods = GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var method in methods)
            {
                if (IsEventHandlerMethod(method))
                {
                    var eventType = method.GetParameters().FirstOrDefault()?.ParameterType;
                    if (eventType != null)
                    {
                        _eventHandlers[eventType] = method;
                        _logger.LogDebug("Discovered event handler for type {EventType}", eventType.Name);
                    }
                }
            }
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
            
            return projectionInterface.IsAssignableFrom(GetType());
        }
    }
}
