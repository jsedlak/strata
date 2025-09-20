using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strata.Eventing;

namespace Strata.Tests.EventHandlers;

[TestClass]
public class SimpleEventHandlerTest
{
    [TestMethod]
    public void EventHandlerRegistry_CanRegisterAndRetrieveHandlers()
    {
        var registry = new EventHandlerRegistry();
        
        // Register a typed handler
        registry.RegisterEventHandler<TestEvent>(@event => Task.CompletedTask);
        
        // Register an untyped handler
        registry.RegisterEventHandler(@event => Task.CompletedTask);
        
        // Get handlers for TestEvent
        var handlers = registry.GetHandlersForEvent<TestEvent>().ToList();
        
        Assert.AreEqual(2, handlers.Count);
    }
    
    [TestMethod]
    public void EventHandlerRegistry_MaintainsRegistrationOrder()
    {
        var registry = new EventHandlerRegistry();
        var callOrder = new List<int>();
        
        // Register handlers in specific order
        registry.RegisterEventHandler<TestEvent>(@event => { callOrder.Add(1); return Task.CompletedTask; });
        registry.RegisterEventHandler<TestEvent>(@event => { callOrder.Add(2); return Task.CompletedTask; });
        registry.RegisterEventHandler<TestEvent>(@event => { callOrder.Add(3); return Task.CompletedTask; });
        
        // Execute handlers
        var handlers = registry.GetHandlersForEvent<TestEvent>();
        foreach (var handler in handlers)
        {
            handler(new TestEvent { Message = "Test" });
        }
        
        Assert.AreEqual(3, callOrder.Count);
        Assert.AreEqual(1, callOrder[0]);
        Assert.AreEqual(2, callOrder[1]);
        Assert.AreEqual(3, callOrder[2]);
    }
}
