using System;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors.DifferentialPrivacy
{
    /// <summary>
    /// Tests for DifferentialPrivacyProcessor privacy budget tracking and management.
    /// Covers budget allocation, depletion, and enforcement.
    /// </summary>
    public class DifferentialPrivacyProcessorPrivacyBudgetTests : DifferentialPrivacyProcessorTestBase
    {
        [Fact]
        public void Constructor_WithPrivacyBudget_ShouldInitializeBudget()
        {
            // Arrange
            var budget = 10.0;
            var config = CreateConfigWithBudget(budget);

            // Act
            var processor = new DifferentialPrivacyProcessor(config);

            // Assert
            Assert.Equal(budget, processor.RemainingBudget);
        }

        [Fact]
        public void AddNoise_WithBudget_ShouldDecrementBudget()
        {
            // Arrange
            var budget = 10.0;
            var epsilon = 1.0;
            var config = CreateConfigWithBudget(budget);
            config["epsilon"] = epsilon;
            var processor = new DifferentialPrivacyProcessor(config);
            var initialBudget = processor.RemainingBudget;

            // Act
            processor.AddNoise(100.0);

            // Assert
            Assert.True(processor.RemainingBudget < initialBudget,
                "Budget should be decremented after adding noise");
            AssertApproximatelyEqual(budget - epsilon, processor.RemainingBudget, 0.001);
        }

        [Fact]
        public void AddNoise_WhenBudgetExhausted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var budget = 1.0;
            var epsilon = 1.0;
            var config = CreateConfigWithBudget(budget);
            config["epsilon"] = epsilon;
            var processor = new DifferentialPrivacyProcessor(config);

            // Act - Exhaust the budget
            processor.AddNoise(100.0);

            // Assert - Should throw on second call
            Assert.Throws<InvalidOperationException>(() => processor.AddNoise(100.0));
        }

        [Fact]
        public void AddNoise_WithInsufficientBudget_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var budget = 0.5;
            var epsilon = 1.0;
            var config = CreateConfigWithBudget(budget);
            config["epsilon"] = epsilon;
            var processor = new DifferentialPrivacyProcessor(config);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => processor.AddNoise(100.0));
        }

        [Fact]
        public void AddNoiseToArray_WithBudget_ShouldDecrementBudgetForEachElement()
        {
            // Arrange
            var budget = 10.0;
            var epsilon = 1.0;
            var config = CreateConfigWithBudget(budget);
            config["epsilon"] = epsilon;
            var processor = new DifferentialPrivacyProcessor(config);
            var values = CreateSequentialTestData(3);

            // Act
            processor.AddNoiseToArray(values);

            // Assert - Budget should be decremented by epsilon * count
            AssertApproximatelyEqual(budget - (epsilon * 3), processor.RemainingBudget, 0.001);
        }

        [Fact]
        public void AddNoiseToArray_WhenBudgetInsufficientForAll_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var budget = 2.0;
            var epsilon = 1.0;
            var config = CreateConfigWithBudget(budget);
            config["epsilon"] = epsilon;
            var processor = new DifferentialPrivacyProcessor(config);
            var values = CreateSequentialTestData(3); // Needs 3.0 budget

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => processor.AddNoiseToArray(values));
        }

        [Fact]
        public void RemainingBudget_WithoutBudgetSet_ShouldReturnInfinity()
        {
            // Arrange
            var config = CreateDefaultConfig();
            var processor = new DifferentialPrivacyProcessor(config);

            // Act & Assert
            Assert.Equal(double.PositiveInfinity, processor.RemainingBudget);
        }

        [Fact]
        public void RemainingBudget_AfterMultipleOperations_ShouldTrackCorrectly()
        {
            // Arrange
            var budget = 10.0;
            var epsilon = 1.0;
            var config = CreateConfigWithBudget(budget);
            config["epsilon"] = epsilon;
            var processor = new DifferentialPrivacyProcessor(config);

            // Act
            processor.AddNoise(100.0); // -1.0
            processor.AddNoise(100.0); // -1.0
            processor.AddNoiseToArray(CreateSequentialTestData(2)); // -2.0

            // Assert
            AssertApproximatelyEqual(6.0, processor.RemainingBudget, 0.001);
        }

        [Fact]
        public void Constructor_WithZeroBudget_ShouldThrowArgumentException()
        {
            // Arrange
            var config = CreateConfigWithBudget(0.0);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DifferentialPrivacyProcessor(config));
        }

        [Fact]
        public void Constructor_WithNegativeBudget_ShouldThrowArgumentException()
        {
            // Arrange
            var config = CreateConfigWithBudget(-1.0);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DifferentialPrivacyProcessor(config));
        }

        [Fact]
        public void ResetBudget_ShouldRestoreBudgetToInitialValue()
        {
            // Arrange
            var budget = 10.0;
            var config = CreateConfigWithBudget(budget);
            var processor = new DifferentialPrivacyProcessor(config);
            processor.AddNoise(100.0); // Use some budget

            // Act
            processor.ResetBudget();

            // Assert
            Assert.Equal(budget, processor.RemainingBudget);
        }
    }
}
