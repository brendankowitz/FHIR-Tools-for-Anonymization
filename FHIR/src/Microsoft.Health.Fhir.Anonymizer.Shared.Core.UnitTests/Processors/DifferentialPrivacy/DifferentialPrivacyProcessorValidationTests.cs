using System;
using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors.DifferentialPrivacy
{
    /// <summary>
    /// Tests for DifferentialPrivacyProcessor input validation and boundary conditions.
    /// Covers parameter validation, edge cases, and error handling.
    /// </summary>
    public class DifferentialPrivacyProcessorValidationTests : DifferentialPrivacyProcessorTestBase
    {
        [Fact]
        public void AddNoise_WithNaN_ShouldThrowArgumentException()
        {
            // Arrange
            var config = CreateDefaultConfig();
            var processor = new DifferentialPrivacyProcessor(config);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => processor.AddNoise(double.NaN));
        }

        [Fact]
        public void AddNoise_WithPositiveInfinity_ShouldThrowArgumentException()
        {
            // Arrange
            var config = CreateDefaultConfig();
            var processor = new DifferentialPrivacyProcessor(config);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => processor.AddNoise(double.PositiveInfinity));
        }

        [Fact]
        public void AddNoise_WithNegativeInfinity_ShouldThrowArgumentException()
        {
            // Arrange
            var config = CreateDefaultConfig();
            var processor = new DifferentialPrivacyProcessor(config);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => processor.AddNoise(double.NegativeInfinity));
        }

        [Fact]
        public void AddNoise_WithMaxValue_ShouldHandleGracefully()
        {
            // Arrange
            var config = CreateDefaultConfig();
            var processor = new DifferentialPrivacyProcessor(config);

            // Act
            var result = processor.AddNoise(double.MaxValue);

            // Assert - Should not overflow or throw
            Assert.True(double.IsFinite(result) || double.IsPositiveInfinity(result));
        }

        [Fact]
        public void AddNoise_WithMinValue_ShouldHandleGracefully()
        {
            // Arrange
            var config = CreateDefaultConfig();
            var processor = new DifferentialPrivacyProcessor(config);

            // Act
            var result = processor.AddNoise(double.MinValue);

            // Assert - Should not underflow or throw
            Assert.True(double.IsFinite(result) || double.IsNegativeInfinity(result));
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

        [Fact]
        public void AddNoiseToArray_WithArrayContainingNaN_ShouldThrowArgumentException()
        {
            // Arrange
            var config = CreateDefaultConfig();
            var processor = new DifferentialPrivacyProcessor(config);
            var values = new[] { 1.0, double.NaN, 3.0 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => processor.AddNoiseToArray(values));
        }

        [Fact]
        public void AddNoiseToArray_WithArrayContainingInfinity_ShouldThrowArgumentException()
        {
            // Arrange
            var config = CreateDefaultConfig();
            var processor = new DifferentialPrivacyProcessor(config);
            var values = new[] { 1.0, double.PositiveInfinity, 3.0 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => processor.AddNoiseToArray(values));
        }

        [Fact]
        public void Constructor_WithEpsilonTooSmall_ShouldThrowArgumentException()
        {
            // Arrange
            var config = CreateConfigWithEpsilon(1e-10);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DifferentialPrivacyProcessor(config));
        }

        [Fact]
        public void Constructor_WithEpsilonTooLarge_ShouldThrowArgumentException()
        {
            // Arrange
            var config = CreateConfigWithEpsilon(1000.0);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DifferentialPrivacyProcessor(config));
        }

        [Fact]
        public void Constructor_WithInvalidConfigurationType_ShouldThrowArgumentException()
        {
            // Arrange
            var config = new Dictionary<string, object>
            {
                ["epsilon"] = "not a number"
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DifferentialPrivacyProcessor(config));
        }

        [Fact]
        public void Constructor_WithMissingRequiredParameter_ShouldUseDefault()
        {
            // Arrange
            var config = new Dictionary<string, object>();

            // Act
            var processor = new DifferentialPrivacyProcessor(config);

            // Assert - Should create successfully with defaults
            Assert.NotNull(processor);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        [InlineData(-100.0)]
        public void Constructor_WithInvalidEpsilonValues_ShouldThrowArgumentException(double epsilon)
        {
            // Arrange
            var config = CreateConfigWithEpsilon(epsilon);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DifferentialPrivacyProcessor(config));
        }

        [Theory]
        [InlineData(-0.1)]
        [InlineData(1.1)]
        [InlineData(2.0)]
        public void Constructor_WithInvalidDeltaValues_ShouldThrowArgumentException(double delta)
        {
            // Arrange
            var config = CreateConfigWithDelta(delta);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DifferentialPrivacyProcessor(config));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        [InlineData(-100.0)]
        public void Constructor_WithInvalidSensitivityValues_ShouldThrowArgumentException(double sensitivity)
        {
            // Arrange
            var config = CreateConfigWithSensitivity(sensitivity);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DifferentialPrivacyProcessor(config));
        }
    }
}
