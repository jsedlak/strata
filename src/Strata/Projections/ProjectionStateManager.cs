using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Strata.Projections
{
    /// <summary>
    /// Manages state for stateful projections.
    /// </summary>
    public class ProjectionStateManager
    {
        private readonly ILogger<ProjectionStateManager> _logger;
        private readonly Dictionary<string, object> _stateCache;
        private readonly Dictionary<string, int> _stateVersions;
        private readonly JsonSerializerOptions _jsonOptions;

        public ProjectionStateManager(ILogger<ProjectionStateManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stateCache = new Dictionary<string, object>();
            _stateVersions = new Dictionary<string, int>();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Gets the current state for a projection.
        /// </summary>
        /// <typeparam name="TState">The type of state.</typeparam>
        /// <param name="projectionId">The projection identifier.</param>
        /// <param name="defaultValue">The default value if no state exists.</param>
        /// <returns>The current state or default value.</returns>
        public TState GetState<TState>(string projectionId, TState defaultValue = default)
        {
            if (string.IsNullOrEmpty(projectionId))
                throw new ArgumentNullException(nameof(projectionId));

            try
            {
                if (_stateCache.TryGetValue(projectionId, out var cachedState) && cachedState is TState state)
                {
                    _logger.LogDebug("Retrieved cached state for projection {ProjectionId}", projectionId);
                    return state;
                }

                _logger.LogDebug("No cached state found for projection {ProjectionId}, returning default", projectionId);
                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving state for projection {ProjectionId}", projectionId);
                return defaultValue;
            }
        }

        /// <summary>
        /// Sets the state for a projection.
        /// </summary>
        /// <typeparam name="TState">The type of state.</typeparam>
        /// <param name="projectionId">The projection identifier.</param>
        /// <param name="state">The state to set.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SetStateAsync<TState>(string projectionId, TState state)
        {
            if (string.IsNullOrEmpty(projectionId))
                throw new ArgumentNullException(nameof(projectionId));

            try
            {
                // Update the cache
                _stateCache[projectionId] = state;

                // Increment version
                _stateVersions[projectionId] = _stateVersions.GetValueOrDefault(projectionId, 0) + 1;

                _logger.LogDebug("Set state for projection {ProjectionId} (version {Version})", 
                    projectionId, _stateVersions[projectionId]);

                // In a real implementation, you would persist the state here
                // For now, we'll just log that persistence would happen
                _logger.LogDebug("State persistence would be implemented here for projection {ProjectionId}", projectionId);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting state for projection {ProjectionId}", projectionId);
                throw;
            }
        }

        /// <summary>
        /// Updates the state for a projection using a transformation function.
        /// </summary>
        /// <typeparam name="TState">The type of state.</typeparam>
        /// <param name="projectionId">The projection identifier.</param>
        /// <param name="updateFunction">The function to update the state.</param>
        /// <param name="defaultValue">The default value if no state exists.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpdateStateAsync<TState>(string projectionId, Func<TState, TState> updateFunction, TState defaultValue = default)
        {
            if (string.IsNullOrEmpty(projectionId))
                throw new ArgumentNullException(nameof(projectionId));
            if (updateFunction == null)
                throw new ArgumentNullException(nameof(updateFunction));

            try
            {
                var currentState = GetState(projectionId, defaultValue);
                var updatedState = updateFunction(currentState);
                await SetStateAsync(projectionId, updatedState);

                _logger.LogDebug("Updated state for projection {ProjectionId}", projectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating state for projection {ProjectionId}", projectionId);
                throw;
            }
        }

        /// <summary>
        /// Clears the state for a projection.
        /// </summary>
        /// <param name="projectionId">The projection identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ClearStateAsync(string projectionId)
        {
            if (string.IsNullOrEmpty(projectionId))
                throw new ArgumentNullException(nameof(projectionId));

            try
            {
                _stateCache.Remove(projectionId);
                _stateVersions.Remove(projectionId);

                _logger.LogDebug("Cleared state for projection {ProjectionId}", projectionId);

                // In a real implementation, you would clear persisted state here
                _logger.LogDebug("State clearing would be implemented here for projection {ProjectionId}", projectionId);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing state for projection {ProjectionId}", projectionId);
                throw;
            }
        }

        /// <summary>
        /// Gets the version of the state for a projection.
        /// </summary>
        /// <param name="projectionId">The projection identifier.</param>
        /// <returns>The state version, or 0 if no state exists.</returns>
        public int GetStateVersion(string projectionId)
        {
            if (string.IsNullOrEmpty(projectionId))
                throw new ArgumentNullException(nameof(projectionId));

            return _stateVersions.GetValueOrDefault(projectionId, 0);
        }

        /// <summary>
        /// Checks if state exists for a projection.
        /// </summary>
        /// <param name="projectionId">The projection identifier.</param>
        /// <returns>True if state exists; otherwise, false.</returns>
        public bool HasState(string projectionId)
        {
            if (string.IsNullOrEmpty(projectionId))
                return false;

            return _stateCache.ContainsKey(projectionId);
        }

        /// <summary>
        /// Gets all projection IDs that have state.
        /// </summary>
        /// <returns>A collection of projection IDs.</returns>
        public IEnumerable<string> GetAllProjectionIds()
        {
            return _stateCache.Keys.ToList();
        }

        /// <summary>
        /// Clears all state for all projections.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ClearAllStateAsync()
        {
            try
            {
                var projectionCount = _stateCache.Count;
                _stateCache.Clear();
                _stateVersions.Clear();

                _logger.LogInformation("Cleared all state for {ProjectionCount} projections", projectionCount);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all state");
                throw;
            }
        }

        /// <summary>
        /// Serializes state to JSON for persistence.
        /// </summary>
        /// <typeparam name="TState">The type of state.</typeparam>
        /// <param name="state">The state to serialize.</param>
        /// <returns>The serialized JSON string.</returns>
        public string SerializeState<TState>(TState state)
        {
            try
            {
                return JsonSerializer.Serialize(state, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serializing state of type {StateType}", typeof(TState).Name);
                throw;
            }
        }

        /// <summary>
        /// Deserializes state from JSON.
        /// </summary>
        /// <typeparam name="TState">The type of state.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized state.</returns>
        public TState DeserializeState<TState>(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<TState>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing state of type {StateType} from JSON", typeof(TState).Name);
                throw;
            }
        }
    }
}
