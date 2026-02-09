using System;
using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.Validation;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Validation
{
    public class KAnonymityValidatorTests
    {
        [Fact]
        public void GivenValidKAnonymizedData_WhenValidating_ThenReturnsValid()
        {
            // Arrange
            var validator = new KAnonymityValidator();
            var equivalenceClasses = new List<List<Dictionary<string, string>>>
            {
                new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { { "age", "30-40" }, { "gender", "M" }, { "zip", "98001" } },
                    new Dictionary<string, string> { { "age", "30-40" }, { "gender", "M" }, { "zip", "98001" } },
                    new Dictionary<string, string> { { "age", "30-40" }, { "gender", "M" }, { "zip", "98001" } }
                },
                new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { { "age", "40-50" }, { "gender", "F" }, { "zip", "98002" } },
                    new Dictionary<string, string> { { "age", "40-50" }, { "gender", "F" }, { "zip", "98002" } }
                }
            };

            // Act
            var report = validator.Validate(equivalenceClasses, kValue: 2);

            // Assert
            Assert.NotNull(report);
            Assert.True(report.IsValid);
            Assert.Empty(report.Violations);
            Assert.Equal(2, report.MinGroupSize);
            Assert.Equal(3, report.MaxGroupSize);
        }

        [Fact]
        public void GivenInvalidKAnonymizedData_WhenValidating_ThenReturnsViolations()
        {
            // Arrange
            var validator = new KAnonymityValidator();
            var equivalenceClasses = new List<List<Dictionary<string, string>>>
            {
                new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { { "age", "30-40" }, { "gender", "M" }, { "zip", "98001" } }
                },
                new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { { "age", "40-50" }, { "gender", "F" }, { "zip", "98002" } },
                    new Dictionary<string, string> { { "age", "40-50" }, { "gender", "F" }, { "zip", "98002" } },
                    new Dictionary<string, string> { { "age", "40-50" }, { "gender", "F" }, { "zip", "98002" } }
                }
            };

            // Act
            var report = validator.Validate(equivalenceClasses, kValue: 2);

            // Assert
            Assert.NotNull(report);
            Assert.False(report.IsValid);
            Assert.Single(report.Violations);
            Assert.Equal(1, report.MinGroupSize);
        }

        [Fact]
        public void GivenEmptyData_WhenValidating_ThenReturnsValidResult()
        {
            // Arrange
            var validator = new KAnonymityValidator();
            var equivalenceClasses = new List<List<Dictionary<string, string>>>();

            // Act
            var report = validator.Validate(equivalenceClasses, kValue: 2);

            // Assert
            Assert.NotNull(report);
            Assert.True(report.IsValid);
            Assert.Empty(report.Violations);
        }

        [Fact]
        public void GivenEquivalenceClasses_WhenComputingDistribution_ThenReturnsCorrectStatistics()
        {
            // Arrange
            var validator = new KAnonymityValidator();
            var equivalenceClasses = new List<List<Dictionary<string, string>>>
            {
                new List<Dictionary<string, string>> { new Dictionary<string, string>(), new Dictionary<string, string>() },
                new List<Dictionary<string, string>> { new Dictionary<string, string>(), new Dictionary<string, string>(), new Dictionary<string, string>() },
                new List<Dictionary<string, string>> { new Dictionary<string, string>(), new Dictionary<string, string>(), new Dictionary<string, string>(), new Dictionary<string, string>() }
            };

            // Act
            var report = validator.Validate(equivalenceClasses, kValue: 2);

            // Assert
            Assert.NotNull(report);
            Assert.Equal(2, report.MinGroupSize);
            Assert.Equal(4, report.MaxGroupSize);
            Assert.Equal(3.0, report.AverageGroupSize, 1);
        }

        [Fact]
        public void GivenLargeKValue_WhenValidating_ThenIdentifiesAllViolations()
        {
            // Arrange
            var validator = new KAnonymityValidator();
            var equivalenceClasses = new List<List<Dictionary<string, string>>>
            {
                new List<Dictionary<string, string>> { new Dictionary<string, string>(), new Dictionary<string, string>() },
                new List<Dictionary<string, string>> { new Dictionary<string, string>(), new Dictionary<string, string>(), new Dictionary<string, string>() },
                new List<Dictionary<string, string>> { new Dictionary<string, string>(), new Dictionary<string, string>(), new Dictionary<string, string>(), new Dictionary<string, string>() }
            };

            // Act
            var report = validator.Validate(equivalenceClasses, kValue: 5);

            // Assert
            Assert.NotNull(report);
            Assert.False(report.IsValid);
            Assert.Equal(3, report.Violations.Count); // All groups violate k=5
        }
    }
}
