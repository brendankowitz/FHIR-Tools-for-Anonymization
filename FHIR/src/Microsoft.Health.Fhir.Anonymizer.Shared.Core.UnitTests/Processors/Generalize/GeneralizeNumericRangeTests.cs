using System;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors.Generalize
{
    /// <summary>
    /// Unit tests for numeric range generalization functionality.
    /// Tests cover range bucketing, boundary conditions, and numeric value handling.
    /// </summary>
    public class GeneralizeNumericRangeTests
    {
        private readonly GeneralizeProcessor _processor;

        public GeneralizeNumericRangeTests()
        {
            _processor = new GeneralizeProcessor();
        }

        [Theory]
        [InlineData(5, "[0,10)")]
        [InlineData(15, "[10,20)")]
        [InlineData(25, "[20,30)")]
        [InlineData(0, "[0,10)")]
        [InlineData(9, "[0,10)")]
        public void GivenNumericValueWithRangeConfig_WhenGeneralize_ThenValueShouldBeInCorrectRange(int value, string expectedRange)
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "[0,10)", "[10,20)", "[20,30)" }
            };

            var node = CreateIntegerNode(value);
            var result = _processor.Process(node, null, settings);

            Assert.NotNull(result);
            Assert.Equal(expectedRange, result.Value.ToString());
        }

        [Fact]
        public void GivenDecimalValue_WhenGeneralizeWithRange_ThenShouldReturnCorrectRange()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "[0.0,10.0)", "[10.0,20.0)", "[20.0,30.0)" }
            };

            var node = CreateDecimalNode(15.5m);
            var result = _processor.Process(node, null, settings);

            Assert.NotNull(result);
            Assert.Equal("[10.0,20.0)", result.Value.ToString());
        }

        [Fact]
        public void GivenValueOutsideRanges_WhenGeneralize_ThenShouldReturnNull()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "[0,10)", "[10,20)" }
            };

            var node = CreateIntegerNode(25);
            var result = _processor.Process(node, null, settings);

            Assert.Null(result);
        }

        [Fact]
        public void GivenNegativeValue_WhenGeneralizeWithNegativeRanges_ThenShouldWork()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "[-20,-10)", "[-10,0)", "[0,10)" }
            };

            var node = CreateIntegerNode(-15);
            var result = _processor.Process(node, null, settings);

            Assert.NotNull(result);
            Assert.Equal("[-20,-10)", result.Value.ToString());
        }

        private ElementNode CreateIntegerNode(int value)
        {
            var integer = new Integer(value);
            return ElementNode.FromElement(integer.ToTypedElement());
        }

        private ElementNode CreateDecimalNode(decimal value)
        {
            var decimalValue = new FhirDecimal(value);
            return ElementNode.FromElement(decimalValue.ToTypedElement());
        }
    }
}
