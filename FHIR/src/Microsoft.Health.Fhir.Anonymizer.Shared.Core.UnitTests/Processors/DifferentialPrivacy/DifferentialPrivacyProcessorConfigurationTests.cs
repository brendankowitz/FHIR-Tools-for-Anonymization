using System;
using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors.DifferentialPrivacy
{
    /// <summary>
    /// Tests for DifferentialPrivacyProcessor configuration handling.
    /// Covers epsilon, delta, sensitivity parameters and configuration validation.
    /// </summary>
    public class DifferentialPrivacyProcessorConfigurationTests : DifferentialPrivacyProcessorTestBase
    {
        [Fact]
        public void Constructor_WithValidEpsilon_ShouldSucceed()
        {
            // Arrange
            var config = CreateConfigWithEpsilon(0.5);

            // Act & Assert
            var exception = Record.Exception(() => new DifferentialPrivacyProcessor(config));
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_WithZeroEpsilon_ShouldThrowArgumentException()
        {
            // Arrange
            var config = CreateConfigWithEpsilon(0.0);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DifferentialPrivacyProcessor(config));
        }

        [Fact]
        public void Constructor_WithNegativeEpsilon_ShouldThrowArgumentException()
        {
            // Arrange
            var config = CreateConfigWithEpsilon(-1.0);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DifferentialPrivacyProcessor(config));
        }

        [Fact]
        public void Constructor_WithValidDelta_ShouldSucceed()
        {
            // Arrange
            var config = CreateConfigWithDelta(1e-6);

            // Act & Assert
            var exception = Record.Exception(() => new DifferentialPrivacyProcessor(config));
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_WithNegativeDelta_ShouldThrowArgumentException()
        {
            // Arrange
            var config = CreateConfigWithDelta(-1e-5);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DifferentialPrivacyProcessor(config));
        }

        [Fact]
        public void Constructor_WithDeltaGreaterThanOne_ShouldThrowArgumentException()
        {
            // Arrange
            var config = CreateConfigWithDelta(1.1);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DifferentialPrivacyProcessor(config));
        }

        [Fact]
        public void Constructor_WithValidSensitivity_ShouldSucceed()
        {
            // Arrange
            var config = CreateConfigWithSensitivity(2.0);

            // Act & Assert
            var exception = Record.Exception(() => new DifferentialPrivacyProcessor(config));
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_WithZeroSensitivity_ShouldThrowArgumentException()
        {
            // Arrange
            var config = CreateConfigWithSensitivity(0.0);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DifferentialPrivacyProcessor(config));
        }

        [Fact]
        public void Constructor_WithNegativeSensitivity_ShouldThrowArgumentException()
        {
            // Arrange
            var config = CreateConfigWithSensitivity(-1.0);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DifferentialPrivacyProcessor(config));
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DifferentialPrivacyProcessor(null));
        }

        [Fact]
        public void Constructor_WithEmptyConfiguration_ShouldUseDefaults()
        {
            // Arrange
            var config = new Dictionary<string, object>();

            // Act & Assert
            var exception = Record.Exception(() => new DifferentialPrivacyProcessor(config));
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_WithValidMechanism_ShouldSucceed()
        {
            // Arrange
            var config = CreateConfigWithMechanism("laplace");

            // Act & Assert
            var exception = Record.Exception(() => new DifferentialPrivacyProcessor(config));
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_WithInvalidMechanism_ShouldThrowArgumentException()
        {
            // Arrange
            var config = CreateConfigWithMechanism("invalid");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DifferentialPrivacyProcessor(config));
        }
    }
}
