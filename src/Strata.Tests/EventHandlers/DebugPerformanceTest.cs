using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strata.Tests.EventHandlers;

namespace Strata.Tests.EventHandlers;

[TestClass]
public class DebugPerformanceTest : OrleansTestBase<DefaultSiloConfigurator>
{
    [TestMethod]
    public async Task Debug_PerformanceTestGrain()
    {
        var grain = Grains.GetGrain<IPerformanceTestGrain>(Guid.NewGuid());
        
        // Check if handlers were registered
        var handlersRegistered = await grain.GetHandlersRegistered();
        var handlerCount = await grain.GetRegisteredHandlerCount();
        
        Console.WriteLine($"Performance grain handlers registered: {handlersRegistered}");
        Console.WriteLine($"Performance grain handler count: {handlerCount}");
        
        Assert.IsTrue(handlersRegistered, "Performance grain handlers should be registered");
        Assert.AreEqual(100, handlerCount, "Performance grain should have 100 handlers");
        
        // Try to raise an event
        var performanceEvent = new PerformanceTestEvent { Id = 1, Data = "Test" };
        await grain.RaisePerformanceEvent(performanceEvent);
        
        // Check if handlers were called
        var state = await grain.GetState();
        var handlerCalls = state.HandlerCalls;
        
        Console.WriteLine($"Performance grain handler calls: {handlerCalls.Count}");
        foreach (var call in handlerCalls.Take(5)) // Show first 5 calls
        {
            Console.WriteLine($"Handler call: {call}");
        }
        
        Assert.IsTrue(handlerCalls.Count > 0, "Performance grain handlers should have been called");
    }
}
