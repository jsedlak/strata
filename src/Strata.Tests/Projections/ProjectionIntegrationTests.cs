using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strata.Projections;

namespace Strata.Tests.Projections
{
    [TestClass]
    public class ProjectionIntegrationTests
    {
        [TestMethod]
        public async Task ProjectionRegistry_ShouldRegisterAndRetrieveProjections()
        {
            // Arrange
            var logger = new TestLogger<ProjectionRegistry>();
            var registry = new ProjectionRegistry(logger);

            // Act
            var registered = registry.RegisterProjection(typeof(TestProjection));

            // Assert
            Assert.IsTrue(registered);
            Assert.IsTrue(registry.HasState("TestProjection"));
        }

        [TestMethod]
        public async Task ProjectionStateManager_ShouldManageState()
        {
            // Arrange
            var logger = new TestLogger<ProjectionStateManager>();
            var stateManager = new ProjectionStateManager(logger);
            var projectionId = "test-projection";
            var testState = new TestState { Value = "test", Count = 42 };

            // Act
            await stateManager.SetStateAsync(projectionId, testState);
            var retrievedState = stateManager.GetState(projectionId, new TestState());

            // Assert
            Assert.AreEqual(testState.Value, retrievedState.Value);
            Assert.AreEqual(testState.Count, retrievedState.Count);
            Assert.IsTrue(stateManager.HasState(projectionId));
            Assert.AreEqual(1, stateManager.GetStateVersion(projectionId));
        }

        [TestMethod]
        public async Task StreamEventProcessor_ShouldProcessEvents()
        {
            // Arrange
            var logger = new TestLogger<StreamEventProcessor>();
            var projection = new TestProjection();
            var processor = new StreamEventProcessor(projection, logger);
            var testEvent = new TestEvent { Value = "stream test" };

            // Act
            await processor.ProcessEventAsync(testEvent);

            // Assert
            Assert.IsTrue(processor.CanHandleEventType(typeof(TestEvent)));
            Assert.IsTrue(projection.HandledEvents.Contains(testEvent));
        }

        [TestMethod]
        public async Task ProjectionStateManager_ShouldUpdateState()
        {
            // Arrange
            var logger = new TestLogger<ProjectionStateManager>();
            var stateManager = new ProjectionStateManager(logger);
            var projectionId = "test-projection";
            var initialState = new TestState { Value = "initial", Count = 0 };

            await stateManager.SetStateAsync(projectionId, initialState);

            // Act
            await stateManager.UpdateStateAsync(projectionId, state => new TestState 
            { 
                Value = state.Value + "-updated", 
                Count = state.Count + 1 
            });

            var updatedState = stateManager.GetState(projectionId, new TestState());

            // Assert
            Assert.AreEqual("initial-updated", updatedState.Value);
            Assert.AreEqual(1, updatedState.Count);
            Assert.AreEqual(2, stateManager.GetStateVersion(projectionId));
        }

        [TestMethod]
        public async Task ProjectionStateManager_ShouldSerializeAndDeserializeState()
        {
            // Arrange
            var logger = new TestLogger<ProjectionStateManager>();
            var stateManager = new ProjectionStateManager(logger);
            var testState = new TestState { Value = "serialization test", Count = 123 };

            // Act
            var json = stateManager.SerializeState(testState);
            var deserializedState = stateManager.DeserializeState<TestState>(json);

            // Assert
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("serialization test"));
            Assert.AreEqual(testState.Value, deserializedState.Value);
            Assert.AreEqual(testState.Count, deserializedState.Count);
        }

        private class TestEvent
        {
            public string Value { get; set; }
        }

        private class TestState
        {
            public string Value { get; set; }
            public int Count { get; set; }
        }

        private class TestProjection : IProjection<TestEvent>
        {
            public System.Collections.Generic.List<TestEvent> HandledEvents { get; } = new();

            public async Task Handle(TestEvent @event)
            {
                HandledEvents.Add(@event);
                await Task.CompletedTask;
            }
        }

        private class TestLogger<T> : ILogger<T>
        {
            public IDisposable BeginScope<TState>(TState state) => null;
            public bool IsEnabled(LogLevel logLevel) => false;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
        }
    }
}
