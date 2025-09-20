using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strata.Eventing;

namespace Strata.Tests.EventHandlers;

[TestClass]
public class DirectEventHandlerTest
{
    [TestMethod]
    public void TestEventHandlerRegistry_DirectUsage()
    {
        var registry = new EventHandlerRegistry();
        var callCount = 0;
        
        // Register handlers
        registry.RegisterEventHandler<TestEvent>(@event => 
        {
            callCount++;
            return Task.CompletedTask;
        });
        
        registry.RegisterEventHandler(@event => 
        {
            callCount++;
            return Task.CompletedTask;
        });
        
        // Verify registration
        Assert.AreEqual(2, registry.HandlerCount);
        
        // Get handlers and execute them
        var handlers = registry.GetHandlersForEvent<TestEvent>().ToList();
        Assert.AreEqual(2, handlers.Count);
        
        var testEvent = new TestEvent { Message = "Test" };
        foreach (var handler in handlers)
        {
            handler(testEvent);
        }
        
        Assert.AreEqual(2, callCount);
    }
}
