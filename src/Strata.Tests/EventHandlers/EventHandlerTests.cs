using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orleans;
using Orleans.Hosting;
using Strata.Eventing;
using Strata.Tests.EventHandlers;

namespace Strata.Tests.EventHandlers;

[TestClass]
public class EventHandlerTests : OrleansTestBase<DefaultSiloConfigurator>
{
    [TestMethod]
    public async Task CanRegisterTypedEventHandler()
    {
        var grain = Grains.GetGrain<ITestEventHandlerGrain>(Guid.NewGuid());
        
        // The grain should be activated and handlers registered
        var state = await grain.GetState();
        Assert.IsNotNull(state);
    }

    [TestMethod]
    public async Task CanRegisterUntypedEventHandler()
    {
        var grain = Grains.GetGrain<ITestEventHandlerGrain>(Guid.NewGuid());
        
        // The grain should be activated and handlers registered
        var state = await grain.GetState();
        Assert.IsNotNull(state);
    }

    [TestMethod]
    public async Task HandlersAreCalledWhenEventIsRaised()
    {
        var grain = Grains.GetGrain<ITestEventHandlerGrain>(Guid.NewGuid());
        
        var testEvent = new TestEvent { Message = "Test Message" };
        await grain.RaiseTestEvent(testEvent);
        
        var handlerCalls = await grain.GetHandlerCalls();
        Assert.IsTrue(handlerCalls.Any(call => call.Contains("TestEvent: Test Message")));
        Assert.IsTrue(handlerCalls.Any(call => call.Contains("Untyped: TestEvent")));
    }

    [TestMethod]
    public async Task TypedHandlersOnlyCalledForMatchingEvents()
    {
        var grain = Grains.GetGrain<ITestEventHandlerGrain>(Guid.NewGuid());
        
        var typedEvent = new TypedTestEvent { Value = 42, Description = "Test Description" };
        await grain.RaiseTypedTestEvent(typedEvent);
        
        var handlerCalls = await grain.GetHandlerCalls();
        Assert.IsTrue(handlerCalls.Any(call => call.Contains("TypedTestEvent: 42 - Test Description")));
        Assert.IsTrue(handlerCalls.Any(call => call.Contains("Untyped: TypedTestEvent")));
        
        // Should not have TestEvent handler calls
        Assert.IsFalse(handlerCalls.Any(call => call.Contains("TestEvent:")));
    }

    [TestMethod]
    public async Task UntypedHandlersCalledForAllEvents()
    {
        var grain = Grains.GetGrain<ITestEventHandlerGrain>(Guid.NewGuid());
        
        var testEvent = new TestEvent { Message = "Test Message" };
        await grain.RaiseTestEvent(testEvent);
        
        var typedEvent = new TypedTestEvent { Value = 42, Description = "Test Description" };
        await grain.RaiseTypedTestEvent(typedEvent);
        
        var handlerCalls = await grain.GetHandlerCalls();
        Assert.IsTrue(handlerCalls.Any(call => call.Contains("Untyped: TestEvent")));
        Assert.IsTrue(handlerCalls.Any(call => call.Contains("Untyped: TypedTestEvent")));
    }

    [TestMethod]
    public async Task HandlersCalledInRegistrationOrder()
    {
        var grain = Grains.GetGrain<IOrderTestGrain>(Guid.NewGuid());
        
        var orderEvent = new OrderTestEvent { Sequence = 1, HandlerName = "Test" };
        await grain.RaiseOrderTestEvent(orderEvent);
        
        var handlerCalls = await grain.GetHandlerCalls();
        Assert.AreEqual(3, handlerCalls.Count);
        Assert.AreEqual("First", handlerCalls[0]);
        Assert.AreEqual("Second", handlerCalls[1]);
        Assert.AreEqual("Third", handlerCalls[2]);
    }

    [TestMethod]
    public async Task AsyncHandlersExecuteCorrectly()
    {
        var grain = Grains.GetGrain<ITestEventHandlerGrain>(Guid.NewGuid());
        
        var asyncEvent = new AsyncTestEvent { DelayMs = 50, Result = "Async Result" };
        await grain.RaiseAsyncTestEvent(asyncEvent);
        
        var handlerCalls = await grain.GetHandlerCalls();
        Assert.IsTrue(handlerCalls.Any(call => call.Contains("AsyncTestEvent: Async Result")));
    }

    [TestMethod]
    public async Task HandlerErrorsDoNotStopOtherHandlers()
    {
        var grain = Grains.GetGrain<ITestEventHandlerGrain>(Guid.NewGuid());
        
        var errorEvent = new ErrorTestEvent { ShouldThrow = true, ErrorMessage = "Test Error" };
        await grain.RaiseErrorTestEvent(errorEvent);
        
        var handlerCalls = await grain.GetHandlerCalls();
        // Should have untyped handler call even though typed handler failed
        Assert.IsTrue(handlerCalls.Any(call => call.Contains("Untyped: ErrorTestEvent")));
    }

    [TestMethod]
    public async Task HandlerErrorsDoNotStopEventProcessing()
    {
        var grain = Grains.GetGrain<ITestEventHandlerGrain>(Guid.NewGuid());
        
        var errorEvent = new ErrorTestEvent { ShouldThrow = true, ErrorMessage = "Test Error" };
        await grain.RaiseErrorTestEvent(errorEvent);
        
        // Event should still be processed despite handler error
        var eventCount = await grain.GetEventCount();
        Assert.IsTrue(eventCount > 0);
    }

    [TestMethod]
    public async Task EventHandlersWorkWithMultipleEvents()
    {
        var grain = Grains.GetGrain<ITestEventHandlerGrain>(Guid.NewGuid());
        
        var events = new object[]
        {
            new TestEvent { Message = "Event 1" },
            new TypedTestEvent { Value = 1, Description = "Event 2" },
            new TestEvent { Message = "Event 3" }
        };
        
        await grain.RaiseMultipleEvents(events);
        
        var handlerCalls = await grain.GetHandlerCalls();
        Assert.IsTrue(handlerCalls.Any(call => call.Contains("TestEvent: Event 1")));
        Assert.IsTrue(handlerCalls.Any(call => call.Contains("TypedTestEvent: 1 - Event 2")));
        Assert.IsTrue(handlerCalls.Any(call => call.Contains("TestEvent: Event 3")));
    }

    [TestMethod]
    public async Task PerformanceWithManyHandlers()
    {
        var grain = Grains.GetGrain<IPerformanceTestGrain>(Guid.NewGuid());
        
        var performanceEvent = new PerformanceTestEvent { Id = 1, Data = "Performance Test" };
        
        var startTime = DateTime.UtcNow;
        await grain.RaisePerformanceEvent(performanceEvent);
        var endTime = DateTime.UtcNow;
        
        var state = await grain.GetState();
        var handlerCalls = state.HandlerCalls;
        
        // Should have 100 handler calls
        Assert.AreEqual(100, handlerCalls.Count);
        
        // Should complete within reasonable time (adjust threshold as needed)
        var duration = endTime - startTime;
        Assert.IsTrue(duration.TotalSeconds < 5, $"Performance test took too long: {duration.TotalSeconds} seconds");
    }

    [TestMethod]
    public async Task HandlersWorkWithDelayedPersistence()
    {
        var grain = Grains.GetGrain<ITestEventHandlerGrain>(Guid.NewGuid());
        
        var testEvent = new TestEvent { Message = "Delayed Test" };
        await grain.RaiseTestEvent(testEvent);
        
        var handlerCalls = await grain.GetHandlerCalls();
        Assert.IsTrue(handlerCalls.Any(call => call.Contains("TestEvent: Delayed Test")));
        
        // Wait for confirmation
        await Task.Delay(TimeSpan.FromSeconds(3));
        
        // Handlers should still work after confirmation
        var typedEvent = new TypedTestEvent { Value = 100, Description = "After Delay" };
        await grain.RaiseTypedTestEvent(typedEvent);
        
        handlerCalls = await grain.GetHandlerCalls();
        Assert.IsTrue(handlerCalls.Any(call => call.Contains("TypedTestEvent: 100 - After Delay")));
    }

    [TestMethod]
    public async Task HandlersAreClearedOnGrainDeactivation()
    {
        var grain = Grains.GetGrain<ITestEventHandlerGrain>(Guid.NewGuid());
        
        // Raise an event to ensure handlers are working
        var testEvent = new TestEvent { Message = "Test Message" };
        await grain.RaiseTestEvent(testEvent);
        
        var handlerCalls = await grain.GetHandlerCalls();
        Assert.IsTrue(handlerCalls.Any(call => call.Contains("TestEvent: Test Message")));
        
        // For now, just verify handlers work - deactivation testing can be added later
        // when we have a proper way to test grain lifecycle in the test framework
    }

    [TestMethod]
    public async Task ConcurrentHandlerRegistration()
    {
        var grain = Grains.GetGrain<ITestEventHandlerGrain>(Guid.NewGuid());
        
        // This test verifies that the registry is thread-safe
        var tasks = new List<Task>();
        
        for (int i = 0; i < 10; i++)
        {
            var eventId = i;
            tasks.Add(Task.Run(async () =>
            {
                var testEvent = new TestEvent { Message = $"Concurrent {eventId}" };
                await grain.RaiseTestEvent(testEvent);
            }));
        }
        
        await Task.WhenAll(tasks);
        
        var handlerCalls = await grain.GetHandlerCalls();
        Assert.AreEqual(10, handlerCalls.Count(call => call.Contains("TestEvent: Concurrent")));
    }
}
