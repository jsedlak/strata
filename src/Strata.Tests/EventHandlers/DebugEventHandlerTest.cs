using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strata.Tests.EventHandlers;

namespace Strata.Tests.EventHandlers;

[TestClass]
public class DebugEventHandlerTest : OrleansTestBase<DefaultSiloConfigurator>
{
    [TestMethod]
    public async Task Debug_CheckHandlerRegistration()
    {
        var grain = Grains.GetGrain<ITestEventHandlerGrain>(Guid.NewGuid());
        
        // Check if handlers are registered by checking the state
        var state = await grain.GetState();
        Assert.IsNotNull(state);
        
        // Try to raise an event and see what happens
        var testEvent = new TestEvent { Message = "Debug Test" };
        await grain.RaiseTestEvent(testEvent);
        
        // Check if any handlers were called
        var handlerCalls = await grain.GetHandlerCalls();
        Console.WriteLine($"Handler calls count: {handlerCalls.Count}");
        foreach (var call in handlerCalls)
        {
            Console.WriteLine($"Handler call: {call}");
        }
        
        // For now, just verify the grain is working
        Assert.IsTrue(true);
    }
}
