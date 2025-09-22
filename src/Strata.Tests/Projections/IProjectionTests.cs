using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strata.Projections;

namespace Strata.Tests.Projections
{
    [TestClass]
    public class IProjectionTests
    {
        [TestMethod]
        public void IProjection_ShouldBeGenericInterface()
        {
            // Arrange & Act
            var interfaceType = typeof(IProjection<>);
            
            // Assert
            Assert.IsTrue(interfaceType.IsGenericType);
            Assert.IsTrue(interfaceType.IsInterface);
            Assert.AreEqual(1, interfaceType.GetGenericArguments().Length);
        }

        [TestMethod]
        public void IProjection_ShouldHaveHandleMethod()
        {
            // Arrange
            var interfaceType = typeof(IProjection<>);
            var handleMethod = interfaceType.GetMethod("Handle");
            
            // Assert
            Assert.IsNotNull(handleMethod);
            Assert.AreEqual("Handle", handleMethod.Name);
            Assert.AreEqual(1, handleMethod.GetParameters().Length);
            Assert.AreEqual(typeof(Task), handleMethod.ReturnType);
        }

        [TestMethod]
        public void IProjection_ShouldBeCovariant()
        {
            // Arrange
            var interfaceType = typeof(IProjection<>);
            var genericParameter = interfaceType.GetGenericArguments()[0];
            
            // Assert
            Assert.IsTrue(genericParameter.GenericParameterAttributes.HasFlag(System.Reflection.GenericParameterAttributes.Contravariant));
        }

        [TestMethod]
        public void IProjection_ShouldWorkWithConcreteImplementation()
        {
            // Arrange
            var projection = new TestProjection();
            var event = new TestEvent { Value = "test" };
            
            // Act & Assert
            Assert.IsInstanceOfType(projection, typeof(IProjection<TestEvent>));
        }

        private class TestEvent
        {
            public string Value { get; set; }
        }

        private class TestProjection : IProjection<TestEvent>
        {
            public Task Handle(TestEvent @event)
            {
                return Task.CompletedTask;
            }
        }
    }
}
