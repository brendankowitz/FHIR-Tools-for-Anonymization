using System;
using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors.DifferentialPrivacy
{
    public class DifferentialPrivacyProcessorConfigurationTests : DifferentialPrivacyProcessorTestBase
    {
        private const int FixedSeed = 54321;

        [Fact]
        public void Process_WithDefaultConfiguration_UsesDefaultValues()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(100.0);
            var settings = new Dictionary<string, object>
            {
                ["seed"] = FixedSeed // Only seed, other values should use defaults
            };

            // Act
            var result = processor.Process(node, settings);

            // Assert - should work with defaults
            Assert.NotNull(result);
            var resultValue = Convert.ToDouble(result.Value);
            Assert.NotEqual(100.0, resultValue); // Should still add noise with defaults
        }

        [Fact]
        public void Process_WithCustomEpsilon_UsesSpecifiedValue()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(100.0);
            var settings = CreateSettings(epsilon: 2.0, sensitivity: 1.0, mechanism: "laplace", seed: FixedSeed);

            // Act
            var result = processor.Process(node, settings);

            // Assert
            Assert.NotNull(result);
            var resultValue = Convert.ToDouble(result.Value);
            Assert.NotEqual(100.0, resultValue);
        }

        [Fact]
        public void Process_WithCustomSensitivity_UsesSpecifiedValue()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(100.0);
            var settings = CreateSettings(epsilon: 1.0, sensitivity: 5.0, mechanism: "laplace", seed: FixedSeed);

            // Act
            var result = processor.Process(node, settings);

            // Assert
            Assert.NotNull(result);
            var resultValue = Convert.ToDouble(result.Value);
            Assert.NotEqual(100.0, resultValue);
        }

        [Fact]
        public void Process_WithLaplaceMechanism_WorksCorrectly()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(50.0);
            var settings = CreateSettings(epsilon: 1.0, sensitivity: 1.0, mechanism: "laplace", seed: FixedSeed);

            // Act
            var result = processor.Process(node, settings);

            // Assert
            Assert.NotNull(result);
            var resultValue = Convert.ToDouble(result.Value);
            Assert.NotEqual(50.0, resultValue);
        }

        [Fact]
        public void Process_WithGaussianMechanism_WorksCorrectly()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(50.0);
            var settings = CreateSettings(epsilon: 1.0, sensitivity: 1.0, mechanism: "gaussian", seed: FixedSeed);

            // Act
            var result = processor.Process(node, settings);

            // Assert
            Assert.NotNull(result);
            var resultValue = Convert.ToDouble(result.Value);
            Assert.NotEqual(50.0, resultValue);
        }

        [Fact]
        public void Process_WithFixedSeed_ProducesReproducibleResults()
        {
            // Arrange
            var processor1 = CreateProcessor();
            var processor2 = CreateProcessor();
            var node1 = CreateNode(100.0);
            var node2 = CreateNode(100.0);
            var settings1 = CreateSettings(epsilon: 1.0, sensitivity: 1.0, mechanism: "laplace", seed: FixedSeed);
            var settings2 = CreateSettings(epsilon: 1.0, sensitivity: 1.0, mechanism: "laplace", seed: FixedSeed);

            // Act
            var result1 = processor1.Process(node1, settings1);
            var result2 = processor2.Process(node2, settings2);

            // Assert - same seed should produce same results
            var value1 = Convert.ToDouble(result1.Value);
            var value2 = Convert.ToDouble(result2.Value);
            Assert.Equal(value1, value2, 6); // Equal to 6 decimal places
        }

        [Fact]
        public void Process_WithVariousEpsilonValues_WorksCorrectly()
        {
            // Arrange
            var processor = CreateProcessor();
            var testEpsilons = new[] { 0.01, 0.1, 0.5, 1.0, 2.0, 5.0, 10.0 };

            foreach (var epsilon in testEpsilons)
            {
                var node = CreateNode(100.0);
                var settings = CreateSettings(epsilon: epsilon, sensitivity: 1.0, mechanism: "laplace", seed: FixedSeed);

                // Act
                var result = processor.Process(node, settings);

                // Assert
                Assert.NotNull(result);
                var resultValue = Convert.ToDouble(result.Value);
                Assert.True(double.IsFinite(resultValue), $"Result should be finite for epsilon={epsilon}");
            }
        }

        [Fact]
        public void Process_WithVariousSensitivityValues_WorksCorrectly()
        {
            // Arrange
            var processor = CreateProcessor();
            var testSensitivities = new[] { 0.1, 0.5, 1.0, 2.0, 5.0, 10.0 };

            foreach (var sensitivity in testSensitivities)
            {
                var node = CreateNode(100.0);
                var settings = CreateSettings(epsilon: 1.0, sensitivity: sensitivity, mechanism: "laplace", seed: FixedSeed);

                // Act
                var result = processor.Process(node, settings);

                // Assert
                Assert.NotNull(result);
                var resultValue = Convert.ToDouble(result.Value);
                Assert.True(double.IsFinite(resultValue), $"Result should be finite for sensitivity={sensitivity}");
            }
        }
    }
}
