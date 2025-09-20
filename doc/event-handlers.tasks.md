# Event Handlers Implementation Tasks

This document outlines the implementation tasks for adding event handler functionality to the Strata EventSourcedGrain, as described in event-handlers.md.

## High-Level Tasks

### 1. Core Event Handler Infrastructure

Implement the foundational event handler system using delegates within the `Strata.Eventing` namespace.

### 2. EventSourcedGrain Integration

Integrate event handler functionality into the existing EventSourcedGrain class.

### 3. Event Processing Pipeline

Implement the event processing pipeline that calls registered handlers when events are raised.

### 4. Comprehensive Testing

Create comprehensive unit tests to ensure complete coverage of event handler functionality.

---

## Detailed Subtasks

### 1. Core Event Handler Infrastructure

#### 1.1 Create Eventing Namespace Structure

- **Task**: Create `Strata.Eventing` namespace directory structure
- **Files to create**:
  - `src/Strata/Eventing/EventHandlerRegistry.cs`
  - `src/Strata/Eventing/EventHandlerDelegate.cs`
- **Details**:
  - Create the namespace folder structure
  - Set up basic class files for event handler management

#### 1.2 Implement EventHandlerDelegate Types

- **Task**: Define delegate types for event handlers
- **File**: `src/Strata/Eventing/EventHandlerDelegate.cs`
- **Details**:
  - Create `EventHandlerDelegate<TEvent>` for typed event handlers
  - Create `EventHandlerDelegate` for untyped event handlers (object parameter)
  - Ensure delegates are async-compatible (return Task)
  - Include XML documentation for both delegate types

#### 1.3 Implement EventHandlerRegistry Class

- **Task**: Create registry to manage event handler registrations
- **File**: `src/Strata/Eventing/EventHandlerRegistry.cs`
- **Details**:
  - Implement thread-safe collection to store registered handlers
  - Support both typed and untyped event handler registration
  - Maintain registration order for sequential execution
  - Include methods:
    - `RegisterEventHandler<TEvent>(EventHandlerDelegate<TEvent> handler)`
    - `RegisterEventHandler(EventHandlerDelegate handler)`
    - `GetHandlersForEvent<TEvent>()` - returns handlers for specific event type
    - `GetAllHandlers()` - returns all untyped handlers
    - `Clear()` - clear all registrations
- **Implementation notes**:
  - Use `ConcurrentBag` or similar thread-safe collection
  - Store handlers with metadata (registration order, handler type)
  - Support both generic and non-generic handler storage

### 2. EventSourcedGrain Integration

#### 2.1 Add Event Handler Support to EventSourcedGrain

- **Task**: Integrate event handler functionality into EventSourcedGrain
- **File**: `src/Strata/EventSourcedGrain.cs`
- **Details**:
  - Add private `EventHandlerRegistry` field
  - Initialize registry in constructor or OnSetup method
  - Add public methods:
    - `RegisterEventHandler<TEvent>(EventHandlerDelegate<TEvent> handler)`
    - `RegisterEventHandler(EventHandlerDelegate handler)`
  - Add protected method:
    - `ProcessEventHandlers(TEventBase @event)` - internal method to process handlers
- **Implementation notes**:
  - Ensure registry is initialized before any handlers can be registered
  - Make registration methods virtual to allow override in derived classes
  - Add proper null checks and error handling

#### 2.2 Integrate Handler Processing into Raise Methods

- **Task**: Modify Raise methods to process event handlers
- **File**: `src/Strata/EventSourcedGrain.cs`
- **Details**:
  - Modify `Raise(TEventBase @event)` method to call `ProcessEventHandlers`
  - Modify `Raise(IEnumerable<TEventBase> events)` method to process handlers for each event
  - Ensure handlers are called before event is submitted to event log
  - Implement error handling: if one handler fails, continue with remaining handlers
- **Implementation notes**:
  - Process handlers in registration order
  - Use try-catch around each handler invocation
  - Log handler failures but don't fail the entire operation
  - Consider adding configuration option to fail-fast on handler errors

#### 2.3 Add Event Handler Processing Logic

- **Task**: Implement the core event handler processing logic
- **File**: `src/Strata/EventSourcedGrain.cs`
- **Details**:
  - Implement `ProcessEventHandlers` method
  - Get all relevant handlers for the event type
  - Call handlers in registration order
  - Handle both typed and untyped handlers
  - Implement proper error handling and logging
- **Implementation notes**:
  - Use reflection or type checking to determine handler compatibility
  - For typed handlers, only call if event type matches
  - For untyped handlers, always call with event as object
  - Consider performance implications of reflection usage

### 3. Event Processing Pipeline

#### 3.1 Implement Handler Invocation Logic

- **Task**: Create robust handler invocation mechanism
- **File**: `src/Strata/EventSourcedGrain.cs` (in ProcessEventHandlers method)
- **Details**:
  - Implement sequential handler execution
  - Add proper async/await handling
  - Implement error isolation (one handler failure doesn't stop others)
  - Add logging for handler execution and failures
- **Implementation notes**:
  - Use `await` for each handler to ensure proper async execution
  - Wrap each handler call in try-catch
  - Log handler start, completion, and any exceptions
  - Consider adding performance metrics/timing

#### 3.2 Add Handler Lifecycle Management

- **Task**: Implement proper cleanup and lifecycle management
- **File**: `src/Strata/EventSourcedGrain.cs`
- **Details**:
  - Clear handlers on grain deactivation
  - Add method to clear specific handlers if needed
  - Ensure proper disposal of handler resources
- **Implementation notes**:
  - Override OnDeactivateAsync to clear handlers
  - Consider implementing IDisposable pattern if needed
  - Add method to unregister specific handlers

#### 3.3 Add Configuration and Options

- **Task**: Add configuration options for event handler behavior
- **File**: `src/Strata/Eventing/EventHandlerOptions.cs`
- **Details**:
  - Create options class for event handler configuration
  - Include options for:
    - Fail-fast on handler errors (default: false)
    - Maximum handler execution time
    - Logging level for handler execution
- **Implementation notes**:
  - Use Microsoft.Extensions.Options pattern
  - Make options configurable via dependency injection
  - Provide sensible defaults

### 4. Comprehensive Testing

#### 4.1 Create Event Handler Test Infrastructure

- **Task**: Set up test infrastructure for event handlers
- **Files to create**:
  - `src/Strata.Tests/EventHandlers/EventHandlerTests.cs`
  - `src/Strata.Tests/EventHandlers/TestEvents.cs`
  - `src/Strata.Tests/EventHandlers/TestGrains.cs`
- **Details**:
  - Create test event classes for various scenarios
  - Create test grain implementations that use event handlers
  - Set up test base class for event handler testing
- **Test events to create**:
  - `TestEvent` - basic test event
  - `TypedTestEvent` - event for typed handler testing
  - `ErrorTestEvent` - event that causes handler errors
  - `AsyncTestEvent` - event for async handler testing

#### 4.2 Test Event Handler Registration

- **Task**: Test event handler registration functionality
- **File**: `src/Strata.Tests/EventHandlers/EventHandlerTests.cs`
- **Test methods**:
  - `CanRegisterTypedEventHandler()` - test typed handler registration
  - `CanRegisterUntypedEventHandler()` - test untyped handler registration
  - `CanRegisterMultipleHandlersForSameEvent()` - test multiple handlers
  - `CanRegisterHandlersForDifferentEvents()` - test different event types
  - `RegistrationOrderIsPreserved()` - test handler execution order
- **Details**:
  - Test both registration methods work correctly
  - Verify handlers are stored in correct order
  - Test that handlers can be registered for different event types
  - Verify no duplicate handler registration issues

#### 4.3 Test Event Handler Execution

- **Task**: Test event handler execution during event raising
- **File**: `src/Strata.Tests/EventHandlers/EventHandlerTests.cs`
- **Test methods**:
  - `HandlersAreCalledWhenEventIsRaised()` - basic execution test
  - `TypedHandlersOnlyCalledForMatchingEvents()` - type-specific execution
  - `UntypedHandlersCalledForAllEvents()` - untyped handler execution
  - `HandlersCalledInRegistrationOrder()` - execution order verification
  - `AsyncHandlersExecuteCorrectly()` - async handler testing
- **Details**:
  - Use mock/capture mechanisms to verify handler calls
  - Test both typed and untyped handler execution
  - Verify execution order matches registration order
  - Test async handler functionality

#### 4.4 Test Error Handling

- **Task**: Test error handling in event handler execution
- **File**: `src/Strata.Tests/EventHandlers/EventHandlerTests.cs`
- **Test methods**:
  - `HandlerErrorsDoNotStopOtherHandlers()` - error isolation test
  - `HandlerErrorsDoNotStopEventProcessing()` - event processing continues
  - `HandlerErrorsAreLogged()` - error logging verification
  - `MultipleHandlerErrorsAreHandled()` - multiple error scenarios
- **Details**:
  - Create handlers that throw exceptions
  - Verify other handlers still execute
  - Verify event is still processed despite handler errors
  - Test logging of handler errors

#### 4.5 Test Integration with EventSourcedGrain

- **Task**: Test event handlers in full EventSourcedGrain context
- **File**: `src/Strata.Tests/EventHandlers/EventHandlerTests.cs`
- **Test methods**:
  - `EventHandlersWorkWithEventSourcing()` - full integration test
  - `EventHandlersWorkWithStateChanges()` - state interaction test
  - `EventHandlersWorkWithMultipleEvents()` - multiple event test
  - `EventHandlersWorkWithDelayedPersistence()` - delayed persistence test
- **Details**:
  - Create test grains that inherit from EventSourcedGrain
  - Test handlers in context of actual event sourcing
  - Verify handlers work with state management
  - Test with both immediate and delayed persistence

#### 4.6 Test Performance and Edge Cases

- **Task**: Test performance and edge cases
- **File**: `src/Strata.Tests/EventHandlers/EventHandlerTests.cs`
- **Test methods**:
  - `PerformanceWithManyHandlers()` - performance with many handlers
  - `PerformanceWithManyEvents()` - performance with many events
  - `HandlersWorkWithNullEvents()` - null event handling
  - `HandlersWorkWithEmptyEventCollections()` - empty collection handling
  - `ConcurrentHandlerRegistration()` - thread safety testing
- **Details**:
  - Test with large numbers of handlers and events
  - Verify thread safety of registration and execution
  - Test edge cases like null events
  - Measure performance impact

#### 4.7 Test Handler Lifecycle Management

- **Task**: Test handler cleanup and lifecycle
- **File**: `src/Strata.Tests/EventHandlers/EventHandlerTests.cs`
- **Test methods**:
  - `HandlersAreClearedOnGrainDeactivation()` - cleanup test
  - `HandlersCanBeClearedManually()` - manual cleanup test
  - `HandlersWorkAfterGrainReactivation()` - reactivation test
- **Details**:
  - Test that handlers are properly cleaned up
  - Test grain deactivation and reactivation scenarios
  - Verify handlers don't persist across grain lifecycles

### 5. Documentation and Examples

#### 5.1 Update EventSourcedGrain Documentation

- **Task**: Update XML documentation for EventSourcedGrain
- **File**: `src/Strata/EventSourcedGrain.cs`
- **Details**:
  - Add XML documentation for new event handler methods
  - Include usage examples in documentation
  - Document error handling behavior
  - Document performance considerations

#### 5.2 Create Usage Examples

- **Task**: Create comprehensive usage examples
- **File**: `src/Strata.Tests/EventHandlers/Examples/`
- **Details**:
  - Create example grain implementations
  - Show different handler registration patterns
  - Demonstrate error handling strategies
  - Show integration with existing event sourcing patterns

#### 5.3 Update Project Documentation

- **Task**: Update project-level documentation
- **Files**:
  - `README.md` - add event handler feature description
  - `doc/event-handlers.md` - ensure accuracy and completeness
- **Details**:
  - Update main README with event handler feature
  - Ensure event-handlers.md is accurate and complete
  - Add links to examples and tests

---

## Implementation Notes

### Design Principles

- **Simplicity**: Use delegates instead of interfaces for maximum simplicity
- **Performance**: Minimize reflection usage and optimize for common cases
- **Reliability**: Ensure handler failures don't break event processing
- **Flexibility**: Support both typed and untyped handlers

### Namespace Organization

- All event handler code should be in `Strata.Eventing` namespace
- Keep implementation details internal to the namespace
- Expose only necessary public APIs

### Error Handling Strategy

- Handler errors should be logged but not fail event processing
- Consider adding configuration option for fail-fast behavior
- Provide clear error messages and stack traces

### Performance Considerations

- Use efficient data structures for handler storage
- Minimize allocations during handler execution
- Consider caching for type checking operations
- Profile performance with realistic handler counts

### Testing Strategy

- Unit tests for individual components
- Integration tests with EventSourcedGrain
- Performance tests with realistic scenarios
- Error scenario testing for robustness

### Backward Compatibility

- All changes should be additive
- No breaking changes to existing EventSourcedGrain API
- Maintain existing behavior when no handlers are registered
