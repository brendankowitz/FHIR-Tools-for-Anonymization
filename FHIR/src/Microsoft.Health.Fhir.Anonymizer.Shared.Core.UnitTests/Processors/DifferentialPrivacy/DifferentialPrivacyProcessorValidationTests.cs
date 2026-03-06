using System;
using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors.DifferentialPrivacy
{
    public class DifferentialPrivacyProcessorValidationTests : DifferentialPrivacyProcessorTestBase
    {
        [Fact]
        public void Process_WithNullNode_ThrowsArgumentNullException()
        {
            // Arrange
            var processor = CreateProcessor();
            var settings = CreateSettings();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => processor.Process(null, settings));
        }

        [Fact]
        public void Process_WithNullSettings_ThrowsArgumentNullException()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(100.0);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => processor.Process(node, null));
        }

        [Fact]
        public void Process_WithNonNumericValue_ThrowsArgumentException()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode("not a number");
            var settings = CreateSettings();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => processor.Process(node, settings));
        }

        [Fact]
        public void Process_WithNullValue_ThrowsArgumentException()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(null);
            var settings = CreateSettings();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => processor.Process(node, settings));
        }

        [Fact]
        public void Process_WithZeroEpsilon_ThrowsArgumentException()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(100.0);
            var settings = CreateSettings(epsilon: 0.0);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => processor.Process(node, settings));
        }

        [Fact]
        public void Process_WithNegativeEpsilon_ThrowsArgumentException()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(100.0);
            var settings = CreateSettings(epsilon: -1.0);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => processor.Process(node, settings));
        }

        [Fact]
        public void Process_WithZeroSensitivity_ThrowsArgumentException()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(100.0);
            var settings = CreateSettings(sensitivity: 0.0);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => processor.Process(node, settings));
        }

        [Fact]
        public void Process_WithNegativeSensitivity_ThrowsArgumentException()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(100.0);
            var settings = CreateSettings(sensitivity: -1.0);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => processor.Process(node, settings));
        }

        [Fact]
        public void Process_WithInvalidMechanism_ThrowsArgumentException()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(100.0);
            var settings = CreateSettings(mechanism: "invalid_mechanism");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => processor.Process(node, settings));
        }

        [Fact]
        public void Process_WithEmptySettings_UsesDefaults()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(100.0);
            var settings = new Dictionary<string, object>();

            // Act - should not throw, should use defaults
            var result = processor.Process(node, settings);

            // Assert
            Assert.NotNull(result);
        }

        [Theory]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        [InlineData(double.NaN)]
        public void Process_WithInvalidEpsilonValue_ThrowsArgumentException(double invalidEpsilon)
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(100.0);
            var settings = CreateSettings(epsilon: invalidEpsilon);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => processor.Process(node, settings));
        }

        [Theory]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        [InlineData(double.NaN)]
        public void Process_WithInvalidSensitivityValue_ThrowsArgumentException(double invalidSensitivity)
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(100.0);
            var settings = CreateSettings(sensitivity: invalidSensitivity);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => processor.Process(node, settings));
        }
    }
}
