using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strata.Tests.EventHandlers;

namespace Strata.Tests.EventHandlers;

[TestClass]
public class DebugSetupTest : OrleansTestBase<DefaultSiloConfigurator>
{
    [TestMethod]
    public async Task Debug_CheckHandlerSetup()
    {
        var grain = Grains.GetGrain<ITestEventHandlerGrain>(Guid.NewGuid());
        
        // Check if handlers were registered during setup
        var handlersRegistered = await grain.GetHandlersRegistered();
        var handlerCount = await grain.GetRegisteredHandlerCount();
        
        Console.WriteLine($"Handlers registered: {handlersRegistered}");
        Console.WriteLine($"Handler count: {handlerCount}");
        
        // This should be true if OnSetupEventHandlers was called
        Assert.IsTrue(handlersRegistered, "Handlers should have been registered during setup");
        Assert.AreEqual(6, handlerCount, "Should have registered 6 handlers");
    }
}
