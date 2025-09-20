using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strata.Eventing;

namespace Strata.Tests.EventHandlers;

[TestClass]
public class UnitEventHandlerTest
{
    [TestMethod]
    public async Task EventHandlerRegistry_CanRegisterAndExecuteHandlers()
    {
        var registry = new EventHandlerRegistry();
        var callCount = 0;
        
        // Register a typed handler
        registry.RegisterEventHandler<TestEvent>(@event => 
        {
            callCount++;
            return Task.CompletedTask;
        });
        
        // Register an untyped handler
        registry.RegisterEventHandler(@event => 
        {
            callCount++;
            return Task.CompletedTask;
        });
        
        // Get handlers for TestEvent
        var handlers = registry.GetHandlersForEvent<TestEvent>().ToList();
        Assert.AreEqual(2, handlers.Count);
        
        // Execute handlers
        var testEvent = new TestEvent { Message = "Test" };
        foreach (var handler in handlers)
        {
            await handler(testEvent);
        }
        
        Assert.AreEqual(2, callCount);
    }
    
    [TestMethod]
    public async Task EventHandlerRegistry_OnlyCallsMatchingTypedHandlers()
    {
        var registry = new EventHandlerRegistry();
        var testEventCalls = 0;
        var typedEventCalls = 0;
        
        // Register handlers for different event types
        registry.RegisterEventHandler<TestEvent>(@event => 
        {
            testEventCalls++;
            return Task.CompletedTask;
        });
        
        registry.RegisterEventHandler<TypedTestEvent>(@event => 
        {
            typedEventCalls++;
            return Task.CompletedTask;
        });
        
        // Register untyped handler
        registry.RegisterEventHandler(@event => 
        {
            if (@event is TestEvent)
                testEventCalls++;
            else if (@event is TypedTestEvent)
                typedEventCalls++;
            return Task.CompletedTask;
        });
        
        // Execute handlers for TestEvent
        var testEventHandlers = registry.GetHandlersForEvent<TestEvent>().ToList();
        var testEvent = new TestEvent { Message = "Test" };
        foreach (var handler in testEventHandlers)
        {
            await handler(testEvent);
        }
        
        // Should have 2 calls for TestEvent (1 typed + 1 untyped)
        Assert.AreEqual(2, testEventCalls);
        Assert.AreEqual(0, typedEventCalls);
    }
}
