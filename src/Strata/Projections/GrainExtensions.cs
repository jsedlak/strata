using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Strata.Projections
{
    /// <summary>
    /// Extension methods for registering projections with EventSourcedGrain.
    /// </summary>
    public static class GrainExtensions
    {
        /// <summary>
        /// Registers a projection type with the EventSourcedGrain.
        /// </summary>
        /// <typeparam name="TProjection">The projection type to register.</typeparam>
        /// <param name="grain">The EventSourcedGrain instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when grain is null.</exception>
        /// <exception cref="ArgumentException">Thrown when TProjection is not a valid projection type.</exception>
        public static void RegisterProjection<TProjection>(this EventSourcedGrain grain) 
            where TProjection : class
        {
            if (grain == null)
                throw new ArgumentNullException(nameof(grain));

            var projectionType = typeof(TProjection);
            var logger = grain.GetLogger<GrainExtensions>();

            try
            {
                // Get all IProjection<TEvent> interfaces that TProjection implements
                var eventTypes = GetAllEventTypes<TProjection>();
                
                if (!eventTypes.Any())
                {
                    throw new ArgumentException($"Type {projectionType.Name} does not implement any IProjection<TEvent> interfaces", nameof(TProjection));
                }

                logger.LogInformation("Registering projection {ProjectionType} for {EventCount} event types", 
                    projectionType.Name, eventTypes.Count);

                // Register event handlers for each event type
                foreach (var eventType in eventTypes)
                {
                    RegisterEventHandlerForProjection(grain, projectionType, eventType, logger);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to register projection {ProjectionType}", projectionType.Name);
                throw;
            }
        }

        /// <summary>
        /// Unregisters a projection type from the EventSourcedGrain.
        /// </summary>
        /// <typeparam name="TProjection">The projection type to unregister.</typeparam>
        /// <param name="grain">The EventSourcedGrain instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when grain is null.</exception>
        public static void UnregisterProjection<TProjection>(this EventSourcedGrain grain) 
            where TProjection : class
        {
            if (grain == null)
                throw new ArgumentNullException(nameof(grain));

            var projectionType = typeof(TProjection);
            var logger = grain.GetLogger<GrainExtensions>();

            try
            {
                var eventTypes = GetAllEventTypes<TProjection>();
                
                logger.LogInformation("Unregistering projection {ProjectionType} for {EventCount} event types", 
                    projectionType.Name, eventTypes.Count);

                // Unregister event handlers for each event type
                foreach (var eventType in eventTypes)
                {
                    UnregisterEventHandlerForProjection(grain, projectionType, eventType, logger);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to unregister projection {ProjectionType}", projectionType.Name);
                throw;
            }
        }

        /// <summary>
        /// Gets all event types that a projection can handle.
        /// </summary>
        private static List<Type> GetAllEventTypes<TProjection>() where TProjection : class
        {
            var projectionType = typeof(TProjection);
            var eventTypes = new List<Type>();

            // Get all interfaces implemented by the projection type
            var interfaces = projectionType.GetInterfaces();

            foreach (var interfaceType in interfaces)
            {
                // Check if it's a generic IProjection<TEvent> interface
                if (interfaceType.IsGenericType && 
                    interfaceType.GetGenericTypeDefinition() == typeof(IProjection<>))
                {
                    var eventType = interfaceType.GetGenericArguments()[0];
                    eventTypes.Add(eventType);
                }
            }

            return eventTypes;
        }

        /// <summary>
        /// Registers an event handler for a specific projection and event type.
        /// </summary>
        private static void RegisterEventHandlerForProjection(
            EventSourcedGrain grain, 
            Type projectionType, 
            Type eventType, 
            ILogger logger)
        {
            try
            {
                // Create a generic method for RegisterEventHandler
                var registerMethod = typeof(EventSourcedGrain)
                    .GetMethods()
                    .FirstOrDefault(m => m.Name == "RegisterEventHandler" && m.IsGenericMethod);

                if (registerMethod == null)
                {
                    throw new InvalidOperationException("RegisterEventHandler method not found on EventSourcedGrain");
                }

                // Make the method generic for the specific event type
                var genericMethod = registerMethod.MakeGenericMethod(eventType);

                // Create the event handler delegate
                var eventHandler = CreateProjectionEventHandler(grain, projectionType, eventType);

                // Register the event handler
                genericMethod.Invoke(grain, new object[] { eventHandler });

                logger.LogDebug("Registered event handler for projection {ProjectionType} and event {EventType}", 
                    projectionType.Name, eventType.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to register event handler for projection {ProjectionType} and event {EventType}", 
                    projectionType.Name, eventType.Name);
                throw;
            }
        }

        /// <summary>
        /// Unregisters an event handler for a specific projection and event type.
        /// </summary>
        private static void UnregisterEventHandlerForProjection(
            EventSourcedGrain grain, 
            Type projectionType, 
            Type eventType, 
            ILogger logger)
        {
            try
            {
                // Create a generic method for UnregisterEventHandler
                var unregisterMethod = typeof(EventSourcedGrain)
                    .GetMethods()
                    .FirstOrDefault(m => m.Name == "UnregisterEventHandler" && m.IsGenericMethod);

                if (unregisterMethod == null)
                {
                    logger.LogWarning("UnregisterEventHandler method not found on EventSourcedGrain");
                    return;
                }

                // Make the method generic for the specific event type
                var genericMethod = unregisterMethod.MakeGenericMethod(eventType);

                // Create the event handler delegate
                var eventHandler = CreateProjectionEventHandler(grain, projectionType, eventType);

                // Unregister the event handler
                genericMethod.Invoke(grain, new object[] { eventHandler });

                logger.LogDebug("Unregistered event handler for projection {ProjectionType} and event {EventType}", 
                    projectionType.Name, eventType.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to unregister event handler for projection {ProjectionType} and event {EventType}", 
                    projectionType.Name, eventType.Name);
                throw;
            }
        }

        /// <summary>
        /// Creates an event handler delegate that forwards events to the projection grain.
        /// </summary>
        private static Delegate CreateProjectionEventHandler(EventSourcedGrain grain, Type projectionType, Type eventType)
        {
            // Create a generic method for the event handler
            var handlerMethod = typeof(GrainExtensions)
                .GetMethod(nameof(CreateProjectionEventHandlerGeneric), BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(eventType);

            return (Delegate)handlerMethod.Invoke(null, new object[] { grain, projectionType });
        }

        /// <summary>
        /// Generic method to create event handler delegates.
        /// </summary>
        private static Func<TEvent, Task> CreateProjectionEventHandlerGeneric<TEvent>(
            EventSourcedGrain grain, 
            Type projectionType) 
            where TEvent : class
        {
            return async (@event) =>
            {
                try
                {
                    // Get the projection grain
                    var projectionGrain = grain.GrainFactory.GetGrain<IProjectionGrain>(
                        $"{grain.GetGrainId()}_{projectionType.Name}");

                    // Apply the projection
                    await projectionGrain.ApplyProjection(@event, projectionType.FullName);
                }
                catch (Exception ex)
                {
                    var logger = grain.GetLogger<GrainExtensions>();
                    logger.LogError(ex, "Error processing projection {ProjectionType} for event {EventType}", 
                        projectionType.Name, typeof(TEvent).Name);
                    throw;
                }
            };
        }
    }
}
