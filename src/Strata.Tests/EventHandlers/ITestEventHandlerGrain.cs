using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Strata.Tests.EventHandlers;

namespace Strata.Tests.EventHandlers;

/// <summary>
/// Interface for the test event handler grain.
/// </summary>
public interface ITestEventHandlerGrain : IGrainWithGuidKey
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

/// <summary>
/// Interface for the performance test grain.
/// </summary>
public interface IPerformanceTestGrain : IGrainWithGuidKey
{
    Task RaisePerformanceEvent(PerformanceTestEvent @event);
    Task<TestState> GetState();
    Task<bool> GetHandlersRegistered();
    Task<int> GetRegisteredHandlerCount();
}

/// <summary>
/// Interface for the order test grain.
/// </summary>
public interface IOrderTestGrain : IGrainWithGuidKey
{
    Task RaiseOrderTestEvent(OrderTestEvent @event);
    Task<List<string>> GetHandlerCalls();
}
