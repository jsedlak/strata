using System.Collections.Concurrent;

namespace Strata;

public sealed class InMemoryEventLog<TView, TEvent> : IEventLog<TView, TEvent>
    where TView : class, new()
    where TEvent : class
{
    private static ConcurrentDictionary<InMemorySnapshotIdentifier, TView> Snapshots = new();
    private static ConcurrentDictionary<InMemoryEventIdentifier, TEvent> Events = new();
    
    private TView _confirmedState = new();
    private TView _tentativeState = new();
    
    private int _confirmedVersion = 0;
    private int _tentativeVersion = 0;

    private readonly InMemoryEventLogOptions _options;
    private readonly IStateSerializer _stateSerializer;
    
    public InMemoryEventLog(InMemoryEventLogOptions options, IStateSerializer stateSerializer)
    {
        _options = options;
        _stateSerializer = stateSerializer;
    }
    
    private void ApplyTentative(TEvent @event)
    {
        dynamic e = @event;
        dynamic s = _tentativeState;
        s.Apply(e);
        _tentativeVersion++;
    }
    
    private void ApplyConfirmed(TEvent @event)
    {
        dynamic e = @event;
        dynamic s = _confirmedState;
        s.Apply(e);
        _confirmedVersion++;
    }

    private TView DeepCopy(TView input)
    {
        var result = _stateSerializer.Serialize(input);
        return _stateSerializer.Deserialize<TView>(result.ToArray());
    }
    
    public Task Hydrate()
    {
        var snapshotKey = Snapshots.Keys
            .Where(m => m.GrainId == _options.GrainId && m.GrainType == _options.GrainType)
            .MaxBy(m => m.Version);

        if (snapshotKey is not null && Snapshots.TryGetValue(snapshotKey, out var snapshot))
        {
            _tentativeState = snapshot;
            _confirmedState = DeepCopy(_tentativeState);
            _confirmedVersion = _tentativeVersion = snapshotKey.Version;
        }

        var tailEventsKeys = Events.Keys
            .Where(m => m.GrainId == _options.GrainId && m.GrainType == _options.GrainType && m.Version > _confirmedVersion)
            .OrderBy(m => m.Version);

        foreach (var eventKey in tailEventsKeys)
        {
            if (!Events.TryGetValue(eventKey, out var tailEvent))
            {
                continue;
            }

            ApplyTentative(tailEvent);
            ApplyConfirmed(tailEvent);
            _confirmedVersion = _tentativeVersion = eventKey.Version;
        }
        
        return Task.CompletedTask;
    }

    public void Submit(TEvent @event)
    {
        ApplyTentative(@event);
    }

    public void Submit(IEnumerable<TEvent> events)
    {
        foreach (var ev in events)
        {
            Submit(ev);
        }
    }

    public Task Snapshot(bool truncate)
    {
        var key = new InMemorySnapshotIdentifier(_options.GrainType, _options.GrainId, _confirmedVersion);

        if (!Snapshots.TryAdd(key, _confirmedState))
        {
            return Task.CompletedTask;
        }

        if (truncate)
        {
            var eventKeysToTruncate = Events.Keys
                .Where(m => m.GrainType == _options.GrainType && m.GrainId == _options.GrainId &&
                            m.Version <= _confirmedVersion)
                .OrderBy(m => m.Version);

            foreach (var keyToTruncate in eventKeysToTruncate)
            {
                Events.Remove(keyToTruncate, out _);
            }
        }

        return Task.CompletedTask;
    }

    public Task WaitForConfirmation()
    {
        _confirmedState = DeepCopy(_tentativeState);
        _confirmedVersion = _tentativeVersion;

        return Task.CompletedTask;
    }

    public TView TentativeView => _tentativeState;

    public TView ConfirmedView => _confirmedState;

    public int ConfirmedVersion => _confirmedVersion;

    public int TentativeVersion => _tentativeVersion;
}

internal record InMemorySnapshotIdentifier(string GrainType, string GrainId, int Version);

internal record InMemoryEventIdentifier(string GrainType, string GrainId, int Version);

public class InMemoryEventLogOptions
{
    public string GrainType { get; set; } = null!;

    public string GrainId { get; set; } = null!;
}