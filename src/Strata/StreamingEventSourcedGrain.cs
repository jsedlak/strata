using System.Reflection;
using Strata;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Orleans.Streams;

namespace Strata;

public abstract class StreamingEventSourcedGrain<TState, TEvent> : EventSourcedGrain<TState, TEvent>
    where TState : class, new()
    where TEvent : class
{
    private readonly string _streamProviderName;

    private readonly string _streamId = null!;

    private IAsyncStream<TEvent>? _eventStream;

    protected StreamingEventSourcedGrain(string streamId, string streamProviderName = "StreamProvider")
    {
        _streamId = streamId;
        _streamProviderName = streamProviderName;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var streamProvider = this.GetStreamProvider(_streamProviderName);

        StreamId? streamId = null;

        if (this is IGrainWithGuidKey)
        {
            streamId = StreamId.Create(_streamId, this.GetPrimaryKey());
        }
        else if (this is IGrainWithStringKey)
        {
            streamId = StreamId.Create(_streamId, this.GetPrimaryKeyString());
        }
        else if (this is IGrainWithIntegerKey)
        {
            streamId = StreamId.Create(_streamId, this.GetPrimaryKeyLong());
        }

        if(streamId is null)
        {
            throw new Exception($"Grain type {GetType().Name} does not implement a valid key interface (IGrainWithGuidKey, IGrainWithStringKey, or IGrainWithIntegerKey).");
        }

        // grab a ref to the stream using the stream id
        _eventStream = streamProvider.GetStream<TEvent>(streamId.Value);

        return base.OnActivateAsync(cancellationToken);
    }

    protected override async Task Raise(TEvent @event)
    {
        await base.Raise(@event);

        if (_eventStream is not null)
        {
            await _eventStream.OnNextAsync(@event);
        }
    }

    protected override async Task Raise(IEnumerable<TEvent> events)
    {
        await base.Raise(events);

        if (_eventStream is not null)
        {
            await _eventStream.OnNextBatchAsync(events);
        }
    }
}