# Projections Implementation Tasks

This document outlines the implementation tasks for adding projection functionality to the Strata Framework, as described in projections.md. The implementation includes two projection types: **Host Managed Projections** and **Projection Managed Grains**.

## High-Level Tasks

### 1. Core Projection Infrastructure

Implement the foundational projection system including interfaces, base classes, and core projection management.

### 2. Host Managed Projections

Implement the ProjectionGrain system that uses Orleans OneWay calls to process projections asynchronously.

### 3. Projection Managed Grains

Implement the EventRecipientGrain system that uses Orleans streams for projection processing.

### 4. Integration with EventSourcedGrain

Extend the existing EventSourcedGrain to support projection registration and management.

### 5. Comprehensive Testing

Create comprehensive unit and integration tests for all projection functionality.

---

## Detailed Subtasks

### 1. Core Projection Infrastructure

#### 1.1 Create Projections Namespace Structure

- **Task**: Create `Strata.Projections` namespace directory structure
- **Files to create**:
  - `src/Strata/Projections/IProjection.cs`
  - `src/Strata/Projections/IProjectionGrain.cs`
  - `src/Strata/Projections/ProjectionGrain.cs`
  - `src/Strata/Projections/EventRecipientGrain.cs`
  - `src/Strata/Projections/GrainExtensions.cs`
  - `src/Strata/Projections/ProjectionOptions.cs`
- **Details**:
  - Create the namespace folder structure
  - Set up basic class files for projection management
  - Follow existing Strata naming conventions

#### 1.2 Implement IProjection Interface

- **Task**: Define the core projection interface
- **File**: `src/Strata/Projections/IProjection.cs`
- **Details**:
  - Create generic `IProjection<TEvent>` interface
  - Define `Handle(TEvent @event)` method signature
  - Add XML documentation
  - Ensure interface is async-compatible (returns Task)

#### 1.3 Implement IProjectionGrain Interface

- **Task**: Define the projection grain interface
- **File**: `src/Strata/Projections/IProjectionGrain.cs`
- **Details**:
  - Create `IProjectionGrain : IGrainWithStringKey`
  - Define `ApplyProjection<TEvent>(TEvent @event, string projectionType)` method
  - Mark method with `[OneWay]` attribute
  - Add generic constraints for type safety
  - Include XML documentation

#### 1.4 Implement ProjectionOptions Configuration

- **Task**: Create configuration options for projections
- **File**: `src/Strata/Projections/ProjectionOptions.cs`
- **Details**:
  - Define configuration properties for projection behavior
  - Include async processing settings
  - Add timeout and retry configuration
  - Support for dead letter queue settings
  - Add validation attributes

### 2. Host Managed Projections

#### 2.1 Implement ProjectionGrain Class

- **Task**: Create the main projection grain implementation
- **File**: `src/Strata/Projections/ProjectionGrain.cs`
- **Details**:
  - Implement `IProjectionGrain` interface
  - Create internal queue mechanism for ordered processing
  - Implement worker thread management
  - Add proper grain lifecycle management
  - Include error handling and logging
  - Support for concurrent projection processing

#### 2.2 Implement GrainExtensions for Projection Registration

- **Task**: Create extension methods for registering projections
- **File**: `src/Strata/Projections/GrainExtensions.cs`
- **Details**:
  - Implement `RegisterProjection<TProjection>()` extension method
  - Add reflection-based event type discovery
  - Create typed event handler registration
  - Use Orleans GrainFactory for better performance
  - Maintain type safety throughout the call chain
  - Add proper error handling

#### 2.3 Implement Projection Processing Pipeline

- **Task**: Create the projection processing infrastructure
- **File**: `src/Strata/Projections/ProjectionProcessor.cs`
- **Details**:
  - Implement async projection processing
  - Add parallel processing support
  - Create error isolation mechanisms
  - Implement retry logic
  - Add metrics and monitoring support

### 3. Projection Managed Grains

#### 3.1 Implement EventRecipientGrain Base Class

- **Task**: Create the base class for stream-based projections
- **File**: `src/Strata/Projections/EventRecipientGrain.cs`
- **Details**:
  - Create abstract base class for projection grains
  - Implement Orleans stream subscription management
  - Add automatic event routing via reflection
  - Create error handling and logging
  - Support for stateful projections
  - Implement proper grain lifecycle management

#### 3.2 Implement Stream-Based Event Processing

- **Task**: Create stream event processing logic
- **File**: `src/Strata/Projections/StreamEventProcessor.cs`
- **Details**:
  - Implement stream event handling
  - Add event type routing
  - Create projection method invocation
  - Add error handling and retry logic
  - Support for event ordering guarantees

#### 3.3 Implement Projection State Management

- **Task**: Create state management for projections
- **File**: `src/Strata/Projections/ProjectionStateManager.cs`
- **Details**:
  - Support for stateless projections
  - Optional stateful projection support
  - State persistence mechanisms
  - State serialization/deserialization
  - State versioning support

### 4. Integration with EventSourcedGrain

#### 4.1 Extend EventSourcedGrain for Projections

- **Task**: Modify EventSourcedGrain to support projections
- **File**: `src/Strata/EventSourcedGrain.cs`
- **Details**:
  - Add projection registration tracking
  - Implement async projection processing
  - Modify `Raise()` method to process projections
  - Add projection error handling
  - Maintain backward compatibility

#### 4.2 Implement Projection Registry

- **Task**: Create registry for managing projection registrations
- **File**: `src/Strata/Projections/ProjectionRegistry.cs`
- **Details**:
  - Track registered projection types
  - Manage projection-to-event mappings
  - Support for dynamic projection registration
  - Thread-safe projection management
  - Performance optimization for projection lookups

#### 4.3 Add Projection Processing to Raise Method

- **Task**: Integrate projection processing into event raising
- **File**: `src/Strata/EventSourcedGrain.cs`
- **Details**:
  - Implement `ProcessProjectionsAsync()` method
  - Add fire-and-forget projection processing
  - Ensure non-blocking event processing
  - Add proper error isolation
  - Maintain event ordering guarantees

### 5. Comprehensive Testing

#### 5.1 Unit Tests for Core Infrastructure

- **Task**: Create unit tests for projection interfaces and base classes
- **Files to create**:
  - `src/Strata.Tests/Projections/IProjectionTests.cs`
  - `src/Strata.Tests/Projections/IProjectionGrainTests.cs`
  - `src/Strata.Tests/Projections/ProjectionOptionsTests.cs`
- **Details**:
  - Test interface contracts
  - Test configuration validation
  - Test error handling scenarios
  - Achieve 100% code coverage for core interfaces

#### 5.2 Unit Tests for Host Managed Projections

- **Task**: Create unit tests for ProjectionGrain and related components
- **Files to create**:
  - `src/Strata.Tests/Projections/ProjectionGrainTests.cs`
  - `src/Strata.Tests/Projections/GrainExtensionsTests.cs`
  - `src/Strata.Tests/Projections/ProjectionProcessorTests.cs`
- **Details**:
  - Test projection grain functionality
  - Test extension method registration
  - Test async processing pipeline
  - Test error handling and retry logic
  - Test concurrent projection processing

#### 5.3 Unit Tests for Projection Managed Grains

- **Task**: Create unit tests for EventRecipientGrain and stream processing
- **Files to create**:
  - `src/Strata.Tests/Projections/EventRecipientGrainTests.cs`
  - `src/Strata.Tests/Projections/StreamEventProcessorTests.cs`
  - `src/Strata.Tests/Projections/ProjectionStateManagerTests.cs`
- **Details**:
  - Test stream subscription management
  - Test event routing and processing
  - Test state management functionality
  - Test error handling scenarios
  - Test grain lifecycle management

#### 5.4 Integration Tests

- **Task**: Create integration tests for complete projection scenarios
- **Files to create**:
  - `src/Strata.Tests/Projections/ProjectionIntegrationTests.cs`
  - `src/Strata.Tests/Projections/StreamProjectionIntegrationTests.cs`
- **Details**:
  - Test end-to-end projection scenarios
  - Test integration with EventSourcedGrain
  - Test Orleans grain communication
  - Test stream-based projections
  - Test error propagation and handling

#### 5.5 Performance Tests

- **Task**: Create performance tests for projection processing
- **Files to create**:
  - `src/Strata.Tests/Projections/ProjectionPerformanceTests.cs`
  - `src/Strata.Tests/Projections/ProjectionLoadTests.cs`
- **Details**:
  - Test projection processing performance
  - Test concurrent projection handling
  - Test memory usage and garbage collection
  - Test throughput under load
  - Benchmark projection processing times

#### 5.6 Orleans Integration Tests

- **Task**: Create tests using Orleans test framework
- **Files to create**:
  - `src/Strata.Tests/OrleansTests/ProjectionTests.cs`
  - `src/Strata.Tests/OrleansTests/StreamProjectionTests.cs`
- **Details**:
  - Test projections in Orleans test environment
  - Test grain activation and deactivation
  - Test stream subscription lifecycle
  - Test Orleans-specific features
  - Test distributed projection scenarios

### 6. Documentation and Examples

#### 6.1 API Documentation

- **Task**: Create comprehensive XML documentation
- **Details**:
  - Document all public interfaces and methods
  - Add usage examples in XML comments
  - Document configuration options
  - Add troubleshooting guides
  - Include performance considerations

#### 6.2 Usage Examples

- **Task**: Create practical usage examples
- **Files to create**:
  - `examples/ProjectionExamples.cs`
  - `examples/StreamProjectionExamples.cs`
- **Details**:
  - Show common projection patterns
  - Demonstrate best practices
  - Show error handling patterns
  - Include performance optimization examples

#### 6.3 Migration Guide

- **Task**: Create migration guide for existing users
- **File**: `doc/projections-migration.md`
- **Details**:
  - Document breaking changes
  - Provide migration steps
  - Show before/after examples
  - Include compatibility notes

### 7. Performance Optimization

#### 7.1 Async Processing Optimization

- **Task**: Optimize async projection processing
- **Details**:
  - Implement efficient task scheduling
  - Optimize memory allocation patterns
  - Add performance counters
  - Implement adaptive concurrency
  - Add performance monitoring

#### 7.2 Memory Management

- **Task**: Optimize memory usage for projections
- **Details**:
  - Implement object pooling where appropriate
  - Optimize serialization/deserialization
  - Add memory usage monitoring
  - Implement efficient state management
  - Add garbage collection optimization

#### 7.3 Caching and State Optimization

- **Task**: Implement caching for projection state
- **Details**:
  - Add projection state caching
  - Implement efficient state lookups
  - Add cache invalidation strategies
  - Optimize state serialization
  - Add cache performance monitoring

---

## Implementation Phases

### Phase 1: Core Infrastructure (Weeks 1-2)

- Implement core projection interfaces
- Create basic projection grain infrastructure
- Set up project structure and namespaces
- Create initial unit tests

### Phase 2: Host Managed Projections (Weeks 3-4)

- Implement ProjectionGrain class
- Create GrainExtensions for registration
- Integrate with EventSourcedGrain
- Add comprehensive unit tests

### Phase 3: Projection Managed Grains (Weeks 5-6)

- Implement EventRecipientGrain base class
- Create stream-based event processing
- Add state management support
- Create integration tests

### Phase 4: Testing and Optimization (Weeks 7-8)

- Complete comprehensive test suite
- Add performance tests and optimization
- Create documentation and examples
- Final integration testing

---

## Dependencies

### Technical Requirements

- .NET 9.0
- Microsoft Orleans 8.0+
- Existing Strata EventSourcedGrain infrastructure
- Orleans Streams support
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Options

### External Dependencies

- Orleans Test Framework for integration testing
- BenchmarkDotNet for performance testing
- xUnit for unit testing
- Moq for mocking in tests

---

## Success Criteria

### Functional Requirements

- [ ] Both Host Managed and Projection Managed projections work correctly
- [ ] Projections can be registered and unregistered dynamically
- [ ] Async processing doesn't block main event flow
- [ ] Error handling isolates projection failures from main processing
- [ ] Stream-based projections maintain event ordering
- [ ] Stateful projections can maintain and persist state

### Performance Requirements

- [ ] Projection processing adds < 10ms overhead to event raising
- [ ] Support for 1000+ concurrent projections per grain
- [ ] Memory usage remains stable under load
- [ ] Projection failures don't impact main event processing performance

### Quality Requirements

- [ ] 100% code coverage for core projection interfaces
- [ ] 90%+ code coverage for projection implementations
- [ ] All tests pass consistently
- [ ] Performance tests meet specified benchmarks
- [ ] Documentation is complete and accurate

---

## Risk Mitigation

### Technical Risks

- **Orleans Integration Complexity**: Use Orleans test framework extensively
- **Performance Impact**: Implement comprehensive performance testing
- **Memory Leaks**: Add memory monitoring and testing
- **Concurrency Issues**: Use Orleans' built-in concurrency controls

### Implementation Risks

- **Scope Creep**: Stick to defined phases and success criteria
- **Testing Gaps**: Implement comprehensive test coverage from start
- **Performance Issues**: Regular performance testing throughout development
- **Integration Problems**: Early and frequent integration testing

---

## Notes

- All projection processing should be non-blocking and fire-and-forget
- Error handling should never impact the main event sourcing flow
- Projections should be designed for high-throughput scenarios
- State management should be optional and configurable
- The implementation should maintain backward compatibility with existing Strata features
