using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strata.Projections;

namespace Strata.Tests.Projections
{
    [TestClass]
    public class ProjectionPerformanceTests
    {
        [TestMethod]
        public async Task ProjectionProcessing_ShouldCompleteWithinReasonableTime()
        {
            // Arrange
            var projection = new TestProjection();
            var testEvent = new TestEvent { Value = "performance test" };
            var stopwatch = Stopwatch.StartNew();

            // Act
            await projection.Handle(testEvent);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, 
                $"Projection processing took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
        }

        [TestMethod]
        public async Task ProjectionProcessing_ShouldHandleConcurrentEvents()
        {
            // Arrange
            var projection = new TestProjection();
            var events = new TestEvent[100];
            for (int i = 0; i < events.Length; i++)
            {
                events[i] = new TestEvent { Value = $"event_{i}" };
            }

            var stopwatch = Stopwatch.StartNew();

            // Act
            var tasks = new Task[events.Length];
            for (int i = 0; i < events.Length; i++)
            {
                var eventIndex = i;
                tasks[i] = Task.Run(async () => await projection.Handle(events[eventIndex]));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, 
                $"Concurrent projection processing took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
        }

        [TestMethod]
        public async Task ProjectionProcessing_ShouldHandleHighThroughput()
        {
            // Arrange
            var projection = new TestProjection();
            var eventCount = 1000;
            var events = new TestEvent[eventCount];
            for (int i = 0; i < events.Length; i++)
            {
                events[i] = new TestEvent { Value = $"high_throughput_event_{i}" };
            }

            var stopwatch = Stopwatch.StartNew();

            // Act
            foreach (var @event in events)
            {
                await projection.Handle(@event);
            }
            stopwatch.Stop();

            // Assert
            var eventsPerSecond = eventCount / (stopwatch.ElapsedMilliseconds / 1000.0);
            Assert.IsTrue(eventsPerSecond > 100, 
                $"Throughput was {eventsPerSecond:F2} events/sec, expected > 100 events/sec");
        }

        [TestMethod]
        public async Task ProjectionProcessing_ShouldHandleMemoryEfficiently()
        {
            // Arrange
            var projection = new TestProjection();
            var eventCount = 10000;
            var initialMemory = GC.GetTotalMemory(true);

            // Act
            for (int i = 0; i < eventCount; i++)
            {
                var @event = new TestEvent { Value = $"memory_test_event_{i}" };
                await projection.Handle(@event);
            }

            GC.Collect();
            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert
            Assert.IsTrue(memoryIncrease < 10 * 1024 * 1024, // 10MB
                $"Memory increase was {memoryIncrease / 1024 / 1024}MB, expected < 10MB");
        }

        private class TestEvent
        {
            public string Value { get; set; }
        }

        private class TestProjection : IProjection<TestEvent>
        {
            public async Task Handle(TestEvent @event)
            {
                // Simulate some processing work
                await Task.Delay(1);
            }
        }
    }
}
