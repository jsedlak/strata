using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strata.Eventing;

namespace Strata.Tests.EventHandlers;

[TestClass]
public class RegistryDebugTest
{
    [TestMethod]
    public void EventHandlerRegistry_DebugPerformanceEvent()
    {
        var registry = new EventHandlerRegistry();
        var callCount = 0;
        
        // Register a handler for PerformanceTestEvent
        registry.RegisterEventHandler<PerformanceTestEvent>(@event => 
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
        
        // Verify registration
        Assert.AreEqual(2, registry.HandlerCount);
        
        // Get handlers for PerformanceTestEvent
        var handlers = registry.GetHandlersForEvent<PerformanceTestEvent>().ToList();
        Assert.AreEqual(2, handlers.Count);
        
        // Execute handlers
        var testEvent = new PerformanceTestEvent { Id = 1, Data = "Test" };
        foreach (var handler in handlers)
        {
            handler(testEvent);
        }
        
        Assert.AreEqual(2, callCount);
    }
    
    [TestMethod]
    public void EventHandlerRegistry_DebugPerformanceEventByType()
    {
        var registry = new EventHandlerRegistry();
        var callCount = 0;
        
        // Register a handler for PerformanceTestEvent
        registry.RegisterEventHandler<PerformanceTestEvent>(@event => 
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
        
        // Get handlers using the non-generic method
        var handlers = registry.GetHandlersForEvent(typeof(PerformanceTestEvent)).ToList();
        Assert.AreEqual(2, handlers.Count);
        
        // Execute handlers
        var testEvent = new PerformanceTestEvent { Id = 1, Data = "Test" };
        foreach (var handler in handlers)
        {
            handler(testEvent);
        }
        
        Assert.AreEqual(2, callCount);
    }
}
