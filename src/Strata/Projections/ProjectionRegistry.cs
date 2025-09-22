using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Strata.Projections
{
    /// <summary>
    /// Registry for managing projection registrations and mappings.
    /// </summary>
    public class ProjectionRegistry
    {
        private readonly ILogger<ProjectionRegistry> _logger;
        private readonly ConcurrentDictionary<Type, List<Type>> _projectionToEventTypes;
        private readonly ConcurrentDictionary<Type, List<Type>> _eventTypeToProjections;
        private readonly ConcurrentDictionary<string, Type> _projectionTypeCache;

        public ProjectionRegistry(ILogger<ProjectionRegistry> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _projectionToEventTypes = new ConcurrentDictionary<Type, List<Type>>();
            _eventTypeToProjections = new ConcurrentDictionary<Type, List<Type>>();
            _projectionTypeCache = new ConcurrentDictionary<string, Type>();
        }

        /// <summary>
        /// Registers a projection type and its associated event types.
        /// </summary>
        /// <param name="projectionType">The projection type to register.</param>
        /// <returns>True if the projection was registered successfully; otherwise, false.</returns>
        public bool RegisterProjection(Type projectionType)
        {
            if (projectionType == null)
                throw new ArgumentNullException(nameof(projectionType));

            try
            {
                var eventTypes = GetEventTypesForProjection(projectionType);
                if (!eventTypes.Any())
                {
                    _logger.LogWarning("Projection type {ProjectionType} does not implement any IProjection<TEvent> interfaces", 
                        projectionType.Name);
                    return false;
                }

                // Register projection to event types mapping
                _projectionToEventTypes.AddOrUpdate(projectionType, eventTypes, (key, existing) => eventTypes);

                // Register event type to projections mapping
                foreach (var eventType in eventTypes)
                {
                    _eventTypeToProjections.AddOrUpdate(
                        eventType,
                        new List<Type> { projectionType },
                        (key, existing) =>
                        {
                            if (!existing.Contains(projectionType))
                            {
                                existing.Add(projectionType);
                            }
                            return existing;
                        });
                }

                // Cache the projection type by name
                _projectionTypeCache.TryAdd(projectionType.FullName, projectionType);

                _logger.LogInformation("Registered projection {ProjectionType} for {EventCount} event types: {EventTypes}", 
                    projectionType.Name, eventTypes.Count, string.Join(", ", eventTypes.Select(t => t.Name)));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register projection {ProjectionType}", projectionType.Name);
                return false;
            }
        }

        /// <summary>
        /// Unregisters a projection type and its associated event types.
        /// </summary>
        /// <param name="projectionType">The projection type to unregister.</param>
        /// <returns>True if the projection was unregistered successfully; otherwise, false.</returns>
        public bool UnregisterProjection(Type projectionType)
        {
            if (projectionType == null)
                throw new ArgumentNullException(nameof(projectionType));

            try
            {
                // Remove projection to event types mapping
                if (_projectionToEventTypes.TryRemove(projectionType, out var eventTypes))
                {
                    // Remove event type to projections mapping
                    foreach (var eventType in eventTypes)
                    {
                        if (_eventTypeToProjections.TryGetValue(eventType, out var projections))
                        {
                            projections.Remove(projectionType);
                            if (!projections.Any())
                            {
                                _eventTypeToProjections.TryRemove(eventType, out _);
                            }
                        }
                    }
                }

                // Remove from cache
                _projectionTypeCache.TryRemove(projectionType.FullName, out _);

                _logger.LogInformation("Unregistered projection {ProjectionType}", projectionType.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister projection {ProjectionType}", projectionType.Name);
                return false;
            }
        }

        /// <summary>
        /// Gets all projection types that can handle the specified event type.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        /// <returns>A list of projection types that can handle the event.</returns>
        public List<Type> GetProjectionsForEventType(Type eventType)
        {
            if (eventType == null)
                throw new ArgumentNullException(nameof(eventType));

            if (_eventTypeToProjections.TryGetValue(eventType, out var projections))
            {
                return new List<Type>(projections);
            }

            return new List<Type>();
        }

        /// <summary>
        /// Gets all event types that the specified projection can handle.
        /// </summary>
        /// <param name="projectionType">The projection type.</param>
        /// <returns>A list of event types that the projection can handle.</returns>
        public List<Type> GetEventTypesForProjection(Type projectionType)
        {
            if (projectionType == null)
                throw new ArgumentNullException(nameof(projectionType));

            if (_projectionToEventTypes.TryGetValue(projectionType, out var eventTypes))
            {
                return new List<Type>(eventTypes);
            }

            // If not cached, discover the event types
            return DiscoverEventTypesForProjection(projectionType);
        }

        /// <summary>
        /// Gets a projection type by its full name.
        /// </summary>
        /// <param name="projectionTypeName">The full name of the projection type.</param>
        /// <returns>The projection type if found; otherwise, null.</returns>
        public Type GetProjectionType(string projectionTypeName)
        {
            if (string.IsNullOrEmpty(projectionTypeName))
                return null;

            if (_projectionTypeCache.TryGetValue(projectionTypeName, out var cachedType))
            {
                return cachedType;
            }

            // Try to find the type by name
            var type = Type.GetType(projectionTypeName);
            if (type != null)
            {
                _projectionTypeCache.TryAdd(projectionTypeName, type);
            }

            return type;
        }

        /// <summary>
        /// Gets all registered projection types.
        /// </summary>
        /// <returns>A list of all registered projection types.</returns>
        public List<Type> GetAllRegisteredProjections()
        {
            return _projectionToEventTypes.Keys.ToList();
        }

        /// <summary>
        /// Gets all registered event types.
        /// </summary>
        /// <returns>A list of all registered event types.</returns>
        public List<Type> GetAllRegisteredEventTypes()
        {
            return _eventTypeToProjections.Keys.ToList();
        }

        /// <summary>
        /// Clears all registrations.
        /// </summary>
        public void Clear()
        {
            _projectionToEventTypes.Clear();
            _eventTypeToProjections.Clear();
            _projectionTypeCache.Clear();
            _logger.LogInformation("Cleared all projection registrations");
        }

        /// <summary>
        /// Discovers event types for a projection by examining its interfaces.
        /// </summary>
        private List<Type> DiscoverEventTypesForProjection(Type projectionType)
        {
            var eventTypes = new List<Type>();

            try
            {
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

                _logger.LogDebug("Discovered {EventCount} event types for projection {ProjectionType}: {EventTypes}", 
                    eventTypes.Count, projectionType.Name, string.Join(", ", eventTypes.Select(t => t.Name)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to discover event types for projection {ProjectionType}", projectionType.Name);
            }

            return eventTypes;
        }
    }
}
