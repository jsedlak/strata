using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Strata.Tests.EventHandlers;

namespace Strata.Tests.EventHandlers;

/// <summary>
/// Interface for the debug test event handler grain.
/// </summary>
public interface ITestEventHandlerGrainDebug : IGrainWithGuidKey
{
    Task RaiseTestEvent(TestEvent @event);
    Task RaiseTypedTestEvent(TypedTestEvent @event);
    Task RaiseErrorTestEvent(ErrorTestEvent @event);
    Task RaiseAsyncTestEvent(AsyncTestEvent @event);
    Task RaiseOrderTestEvent(OrderTestEvent @event);
    Task RaiseMultipleEvents(IEnumerable<object> events);
    Task<TestState> GetState();
    Task<List<string>> GetHandlerCalls();
    Task<int> GetEventCount();
    Task<bool> GetHandlersRegistered();
    Task<int> GetRegisteredHandlerCount();
}
