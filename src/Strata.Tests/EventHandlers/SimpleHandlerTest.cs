using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strata.Tests.EventHandlers;

namespace Strata.Tests.EventHandlers;

[TestClass]
public class SimpleHandlerTest : OrleansTestBase<DefaultSiloConfigurator>
{
    [TestMethod]
    public async Task Simple_TestHandlerExecution()
    {
        var grain = Grains.GetGrain<ITestEventHandlerGrain>(Guid.NewGuid());
        
        // Verify handlers were registered
        var handlersRegistered = await grain.GetHandlersRegistered();
        var handlerCount = await grain.GetRegisteredHandlerCount();
        
        Assert.IsTrue(handlersRegistered, "Handlers should be registered");
        Assert.AreEqual(6, handlerCount, "Should have 6 handlers registered");
        
        // Create a simple test event
        var testEvent = new TestEvent { Message = "Simple Test" };
        
        // Raise the event
        await grain.RaiseTestEvent(testEvent);
        
        // Check if any handlers were called
        var handlerCalls = await grain.GetHandlerCalls();
        var eventCount = await grain.GetEventCount();
        
        Console.WriteLine($"Handler calls: {handlerCalls.Count}");
        Console.WriteLine($"Event count: {eventCount}");
        
        foreach (var call in handlerCalls)
        {
            Console.WriteLine($"Handler call: {call}");
        }
        
        // For now, just verify the grain is working
        // The handlers should have been called
        Assert.IsTrue(handlerCalls.Count > 0, "Handlers should have been called");
    }
}
