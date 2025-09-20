# Strata Event Handlers

This document describes event handlers, a dynamic approach to handling events from within an `EventSourcedGrain`.

## Registering an Event Handler

In an Event Sourced Grain, call the `RegisterEventHandler` method. You can pass in an an event type if you want to handle only that particular event. Or you can pass in a delegate that accepts an `object` parameter if you wish to handle every event.

```csharp
public override virtual Task OnActivateAsync() {
    this.RegisterEventHandler<AddAmountEvent>((@event) => {
        // ... do something with the event
    });

    this.RegisterEventHandler((@event) => {
        if(@event is AddAmountEvent addAmountEvent) {
            // ... do something
        }
    })
}
```

When `RaiseEvent` is called, all handlers are called in the order they were registered. If one event handler fails, the processing continues.
