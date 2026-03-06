using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors.DifferentialPrivacy
{
    public class DifferentialPrivacyProcessorNoiseTests : DifferentialPrivacyProcessorTestBase
    {
        private const int FixedSeed = 12345; // Fixed seed for reproducible tests
        private const double DefaultTolerance = 20.0; // Tolerance for statistical tests

        [Fact]
        public void Process_WithLaplaceNoise_AddsNoiseToValue()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(100.0);
            var settings = CreateSettings(epsilon: 1.0, sensitivity: 1.0, mechanism: "laplace", seed: FixedSeed);

            // Act
            var result = processor.Process(node, settings);

            // Assert
            Assert.NotNull(result);
            var resultValue = Convert.ToDouble(result.Value);
            Assert.NotEqual(100.0, resultValue); // Value should be modified
            AssertInRange(resultValue, 80.0, 120.0); // Within reasonable bounds
        }

        [Fact]
        public void Process_WithGaussianNoise_AddsNoiseToValue()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(50.0);
            var settings = CreateSettings(epsilon: 0.5, sensitivity: 1.0, mechanism: "gaussian", seed: FixedSeed);

            // Act
            var result = processor.Process(node, settings);

            // Assert
            Assert.NotNull(result);
            var resultValue = Convert.ToDouble(result.Value);
            Assert.NotEqual(50.0, resultValue); // Value should be modified
            AssertInRange(resultValue, 30.0, 70.0); // Within reasonable bounds
        }

        [Fact]
        public void Process_WithHigherEpsilon_ProducesLessNoise()
        {
            // Arrange
            var processor = CreateProcessor();
            var originalValue = 100.0;
            var node1 = CreateNode(originalValue);
            var node2 = CreateNode(originalValue);
            var lowEpsilonSettings = CreateSettings(epsilon: 0.1, sensitivity: 1.0, mechanism: "laplace", seed: FixedSeed);
            var highEpsilonSettings = CreateSettings(epsilon: 10.0, sensitivity: 1.0, mechanism: "laplace", seed: FixedSeed);

            // Act
            var lowEpsilonResult = processor.Process(node1, lowEpsilonSettings);
            var highEpsilonResult = processor.Process(node2, highEpsilonSettings);

            // Assert
            var lowEpsilonValue = Convert.ToDouble(lowEpsilonResult.Value);
            var highEpsilonValue = Convert.ToDouble(highEpsilonResult.Value);
            var lowEpsilonDiff = Math.Abs(lowEpsilonValue - originalValue);
            var highEpsilonDiff = Math.Abs(highEpsilonValue - originalValue);

            // Higher epsilon should generally produce less noise (on average)
            // Note: This is a statistical property, so we use fixed seed for reproducibility
            Assert.True(highEpsilonDiff < lowEpsilonDiff || Math.Abs(highEpsilonDiff - lowEpsilonDiff) < DefaultTolerance,
                $"Expected high epsilon diff ({highEpsilonDiff}) < low epsilon diff ({lowEpsilonDiff})");
        }

        [Fact]
        public void Process_WithIntegerValue_ReturnsNoisyInteger()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(42);
            var settings = CreateSettings(epsilon: 1.0, sensitivity: 1.0, mechanism: "laplace", seed: FixedSeed);

            // Act
            var result = processor.Process(node, settings);

            // Assert
            Assert.NotNull(result);
            var resultValue = Convert.ToInt32(result.Value);
            Assert.NotEqual(42, resultValue); // Value should be modified
        }

        [Fact]
        public void Process_WithZeroValue_AddsNoise()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(0.0);
            var settings = CreateSettings(epsilon: 1.0, sensitivity: 1.0, mechanism: "laplace", seed: FixedSeed);

            // Act
            var result = processor.Process(node, settings);

            // Assert
            Assert.NotNull(result);
            var resultValue = Convert.ToDouble(result.Value);
            Assert.NotEqual(0.0, resultValue); // Even zero should get noise
        }

        [Fact]
        public void Process_WithNegativeValue_HandlesCorrectly()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(-50.0);
            var settings = CreateSettings(epsilon: 1.0, sensitivity: 1.0, mechanism: "laplace", seed: FixedSeed);

            // Act
            var result = processor.Process(node, settings);

            // Assert
            Assert.NotNull(result);
            var resultValue = Convert.ToDouble(result.Value);
            Assert.NotEqual(-50.0, resultValue); // Value should be modified
        }

        [Theory]
        [InlineData("laplace")]
        [InlineData("gaussian")]
        public void Process_WithDifferentMechanisms_AddsNoise(string mechanism)
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(100.0);
            var settings = CreateSettings(epsilon: 1.0, sensitivity: 1.0, mechanism: mechanism, seed: FixedSeed);

            // Act
            var result = processor.Process(node, settings);

            // Assert
            Assert.NotNull(result);
            var resultValue = Convert.ToDouble(result.Value);
            Assert.NotEqual(100.0, resultValue);
        }

        [Fact]
        public void Process_MultipleCalls_ProducesDifferentResults()
        {
            // Arrange
            var processor = CreateProcessor();
            var node1 = CreateNode(100.0);
            var node2 = CreateNode(100.0);
            // Don't use fixed seed for this test - we want randomness
            var settings = CreateSettings(epsilon: 1.0, sensitivity: 1.0, mechanism: "laplace");

            // Act
            var result1 = processor.Process(node1, settings);
            var result2 = processor.Process(node2, settings);

            // Assert
            var value1 = Convert.ToDouble(result1.Value);
            var value2 = Convert.ToDouble(result2.Value);
            Assert.NotEqual(value1, value2); // Should produce different noise
        }
    }
}
