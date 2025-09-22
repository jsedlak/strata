using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strata.Projections;

namespace Strata.Tests.Projections
{
    [TestClass]
    public class ProjectionOptionsTests
    {
        [TestMethod]
        public void ProjectionOptions_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var options = new ProjectionOptions();
            
            // Assert
            Assert.AreEqual(10, options.MaxConcurrency);
            Assert.AreEqual(30000, options.ProcessingTimeoutMs);
            Assert.AreEqual(3, options.MaxRetryAttempts);
            Assert.AreEqual(1000, options.RetryDelayMs);
            Assert.IsTrue(options.EnableDeadLetterQueue);
            Assert.AreEqual(10000, options.MaxQueueSize);
            Assert.IsTrue(options.EnablePerformanceCounters);
            Assert.AreEqual(10, options.BatchSize);
        }

        [TestMethod]
        public void ProjectionOptions_ShouldAllowCustomValues()
        {
            // Arrange & Act
            var options = new ProjectionOptions
            {
                MaxConcurrency = 20,
                ProcessingTimeoutMs = 60000,
                MaxRetryAttempts = 5,
                RetryDelayMs = 2000,
                EnableDeadLetterQueue = false,
                MaxQueueSize = 50000,
                EnablePerformanceCounters = false,
                BatchSize = 25
            };
            
            // Assert
            Assert.AreEqual(20, options.MaxConcurrency);
            Assert.AreEqual(60000, options.ProcessingTimeoutMs);
            Assert.AreEqual(5, options.MaxRetryAttempts);
            Assert.AreEqual(2000, options.RetryDelayMs);
            Assert.IsFalse(options.EnableDeadLetterQueue);
            Assert.AreEqual(50000, options.MaxQueueSize);
            Assert.IsFalse(options.EnablePerformanceCounters);
            Assert.AreEqual(25, options.BatchSize);
        }

        [TestMethod]
        public void ProjectionOptions_ShouldValidateRangeAttributes()
        {
            // Arrange
            var options = new ProjectionOptions();
            var context = new ValidationContext(options);
            var results = new System.Collections.Generic.List<ValidationResult>();
            
            // Act
            var isValid = Validator.TryValidateObject(options, context, results, true);
            
            // Assert
            Assert.IsTrue(isValid, "Default options should be valid");
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void ProjectionOptions_ShouldValidateMaxConcurrencyRange()
        {
            // Arrange
            var options = new ProjectionOptions { MaxConcurrency = 0 };
            var context = new ValidationContext(options);
            var results = new System.Collections.Generic.List<ValidationResult>();
            
            // Act
            var isValid = Validator.TryValidateObject(options, context, results, true);
            
            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(results.Count > 0);
        }

        [TestMethod]
        public void ProjectionOptions_ShouldValidateProcessingTimeoutRange()
        {
            // Arrange
            var options = new ProjectionOptions { ProcessingTimeoutMs = 500 };
            var context = new ValidationContext(options);
            var results = new System.Collections.Generic.List<ValidationResult>();
            
            // Act
            var isValid = Validator.TryValidateObject(options, context, results, true);
            
            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(results.Count > 0);
        }

        [TestMethod]
        public void ProjectionOptions_ShouldValidateMaxRetryAttemptsRange()
        {
            // Arrange
            var options = new ProjectionOptions { MaxRetryAttempts = 15 };
            var context = new ValidationContext(options);
            var results = new System.Collections.Generic.List<ValidationResult>();
            
            // Act
            var isValid = Validator.TryValidateObject(options, context, results, true);
            
            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(results.Count > 0);
        }

        [TestMethod]
        public void ProjectionOptions_ShouldValidateRetryDelayRange()
        {
            // Arrange
            var options = new ProjectionOptions { RetryDelayMs = 50 };
            var context = new ValidationContext(options);
            var results = new System.Collections.Generic.List<ValidationResult>();
            
            // Act
            var isValid = Validator.TryValidateObject(options, context, results, true);
            
            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(results.Count > 0);
        }

        [TestMethod]
        public void ProjectionOptions_ShouldValidateMaxQueueSizeRange()
        {
            // Arrange
            var options = new ProjectionOptions { MaxQueueSize = 50 };
            var context = new ValidationContext(options);
            var results = new System.Collections.Generic.List<ValidationResult>();
            
            // Act
            var isValid = Validator.TryValidateObject(options, context, results, true);
            
            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(results.Count > 0);
        }

        [TestMethod]
        public void ProjectionOptions_ShouldValidateBatchSizeRange()
        {
            // Arrange
            var options = new ProjectionOptions { BatchSize = 0 };
            var context = new ValidationContext(options);
            var results = new System.Collections.Generic.List<ValidationResult>();
            
            // Act
            var isValid = Validator.TryValidateObject(options, context, results, true);
            
            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(results.Count > 0);
        }
    }
}
