using System;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors.DifferentialPrivacy
{
    /// <summary>
    /// Tests for DifferentialPrivacyProcessor noise addition mechanisms.
    /// Covers Laplace and Gaussian noise mechanisms.
    /// </summary>
    public class DifferentialPrivacyProcessorNoiseTests : DifferentialPrivacyProcessorTestBase
    {
        [Fact]
        public void AddNoise_WithLaplaceMechanism_ShouldAddNoise()
        {
            // Arrange
            var config = CreateConfigWithMechanism("laplace");
            var processor = new DifferentialPrivacyProcessor(config);
            var originalValue = 100.0;

            // Act
            var noisedValue = processor.AddNoise(originalValue);

            // Assert
            AssertNoiseAdded(originalValue, noisedValue);
        }

        [Fact]
        public void AddNoise_WithGaussianMechanism_ShouldAddNoise()
        {
            // Arrange
            var config = CreateConfigWithMechanism("gaussian");
            var processor = new DifferentialPrivacyProcessor(config);
            var originalValue = 100.0;

            // Act
            var noisedValue = processor.AddNoise(originalValue);

            // Assert
            AssertNoiseAdded(originalValue, noisedValue);
        }

        [Fact]
        public void AddNoise_MultipleCalls_ShouldProduceDifferentValues()
        {
            // Arrange
            var config = CreateDefaultConfig();
            var processor = new DifferentialPrivacyProcessor(config);
            var originalValue = 100.0;

            // Act
            var noisedValue1 = processor.AddNoise(originalValue);
            var noisedValue2 = processor.AddNoise(originalValue);
            var noisedValue3 = processor.AddNoise(originalValue);

            // Assert - At least one should be different (stochastic test)
            Assert.True(noisedValue1 != noisedValue2 || noisedValue2 != noisedValue3,
                "Multiple noise additions should produce different values");
        }

        [Fact]
        public void AddNoise_WithHighEpsilon_ShouldAddLessNoise()
        {
            // Arrange
            var lowEpsilonConfig = CreateConfigWithEpsilon(0.1);
            var highEpsilonConfig = CreateConfigWithEpsilon(10.0);
            var lowEpsilonProcessor = new DifferentialPrivacyProcessor(lowEpsilonConfig);
            var highEpsilonProcessor = new DifferentialPrivacyProcessor(highEpsilonConfig);
            var originalValue = 100.0;
            var iterations = 100;

            // Act - Calculate average deviation for multiple runs
            double lowEpsilonDeviation = 0;
            double highEpsilonDeviation = 0;

            for (int i = 0; i < iterations; i++)
            {
                lowEpsilonDeviation += Math.Abs(lowEpsilonProcessor.AddNoise(originalValue) - originalValue);
                highEpsilonDeviation += Math.Abs(highEpsilonProcessor.AddNoise(originalValue) - originalValue);
            }

            lowEpsilonDeviation /= iterations;
            highEpsilonDeviation /= iterations;

            // Assert - Higher epsilon should result in lower average deviation
            Assert.True(highEpsilonDeviation < lowEpsilonDeviation,
                $"High epsilon deviation ({highEpsilonDeviation}) should be less than low epsilon deviation ({lowEpsilonDeviation})");
        }

        [Fact]
        public void AddNoise_WithHighSensitivity_ShouldAddMoreNoise()
        {
            // Arrange
            var lowSensitivityConfig = CreateConfigWithSensitivity(0.1);
            var highSensitivityConfig = CreateConfigWithSensitivity(10.0);
            var lowSensitivityProcessor = new DifferentialPrivacyProcessor(lowSensitivityConfig);
            var highSensitivityProcessor = new DifferentialPrivacyProcessor(highSensitivityConfig);
            var originalValue = 100.0;
            var iterations = 100;

            // Act - Calculate average deviation for multiple runs
            double lowSensitivityDeviation = 0;
            double highSensitivityDeviation = 0;

            for (int i = 0; i < iterations; i++)
            {
                lowSensitivityDeviation += Math.Abs(lowSensitivityProcessor.AddNoise(originalValue) - originalValue);
                highSensitivityDeviation += Math.Abs(highSensitivityProcessor.AddNoise(originalValue) - originalValue);
            }

            lowSensitivityDeviation /= iterations;
            highSensitivityDeviation /= iterations;

            // Assert - Higher sensitivity should result in higher average deviation
            Assert.True(highSensitivityDeviation > lowSensitivityDeviation,
                $"High sensitivity deviation ({highSensitivityDeviation}) should be greater than low sensitivity deviation ({lowSensitivityDeviation})");
        }

        [Fact]
        public void AddNoise_ToZero_ShouldProduceNoisedValue()
        {
            // Arrange
            var config = CreateDefaultConfig();
            var processor = new DifferentialPrivacyProcessor(config);
            var originalValue = 0.0;

            // Act
            var noisedValue = processor.AddNoise(originalValue);

            // Assert - Noise should be added even to zero
            Assert.NotEqual(0.0, noisedValue);
        }

        [Fact]
        public void AddNoise_ToNegativeValue_ShouldWork()
        {
            // Arrange
            var config = CreateDefaultConfig();
            var processor = new DifferentialPrivacyProcessor(config);
            var originalValue = -50.0;

            // Act & Assert - Should not throw
            var exception = Record.Exception(() => processor.AddNoise(originalValue));
            Assert.Null(exception);
        }

        [Fact]
        public void AddNoiseToArray_ShouldAddNoiseToAllElements()
        {
            // Arrange
            var config = CreateDefaultConfig();
            var processor = new DifferentialPrivacyProcessor(config);
            var originalValues = CreateSequentialTestData(5);

            // Act
            var noisedValues = processor.AddNoiseToArray(originalValues);

            // Assert
            Assert.Equal(originalValues.Length, noisedValues.Length);
            for (int i = 0; i < originalValues.Length; i++)
            {
                AssertNoiseAdded(originalValues[i], noisedValues[i]);
            }
        }

        [Fact]
        public void AddNoiseToArray_WithEmptyArray_ShouldReturnEmptyArray()
        {
            // Arrange
            var config = CreateDefaultConfig();
            var processor = new DifferentialPrivacyProcessor(config);
            var originalValues = new double[0];

            // Act
            var noisedValues = processor.AddNoiseToArray(originalValues);

            // Assert
            Assert.Empty(noisedValues);
        }

        [Fact]
        public void AddNoiseToArray_WithNullArray_ShouldThrowArgumentNullException()
        {
            // Arrange
            var config = CreateDefaultConfig();
            var processor = new DifferentialPrivacyProcessor(config);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => processor.AddNoiseToArray(null));
        }
    }
}
