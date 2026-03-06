using System;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors.Generalize
{
    /// <summary>
    /// Unit tests for postal code generalization functionality.
    /// Tests cover postal code truncation and ZIP code prefix extraction (HIPAA Safe Harbor compliant).
    /// </summary>
    public class GeneralizePostalCodeTests
    {
        private readonly GeneralizeProcessor _processor;

        public GeneralizePostalCodeTests()
        {
            _processor = new GeneralizeProcessor();
        }

        [Theory]
        [InlineData("12345", "123")]
        [InlineData("98765-4321", "987")]
        [InlineData("00123", "001")]
        public void GivenUSZipCode_WhenGeneralizeTo3Digits_ThenShouldReturnFirst3Digits(string zipCode, string expected)
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "first3" }
            };

            var node = CreateStringNode(zipCode);
            var result = _processor.Process(node, null, settings);

            Assert.NotNull(result);
            Assert.Equal(expected, result.Value.ToString());
        }

        [Fact]
        public void GivenShortZipCode_WhenGeneralizeTo3Digits_ThenShouldHandleGracefully()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "first3" }
            };

            var node = CreateStringNode("12");
            var result = _processor.Process(node, null, settings);

            Assert.NotNull(result);
            Assert.Equal("12", result.Value.ToString());
        }

        [Theory]
        [InlineData("K1A 0B1", "K1A")]
        [InlineData("M5H 2N2", "M5H")]
        public void GivenCanadianPostalCode_WhenGeneralizeTo3Chars_ThenShouldReturnFSA(string postalCode, string expected)
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "first3" }
            };

            var node = CreateStringNode(postalCode);
            var result = _processor.Process(node, null, settings);

            Assert.NotNull(result);
            Assert.Equal(expected, result.Value.ToString());
        }

        [Fact]
        public void GivenPostalCode_WhenGeneralizeToPrefix_ThenShouldRemoveSuffix()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "prefix" }
            };

            var node = CreateStringNode("98765-4321");
            var result = _processor.Process(node, null, settings);

            Assert.NotNull(result);
            Assert.Equal("98765", result.Value.ToString());
        }

        [Fact]
        public void GivenEmptyPostalCode_WhenGeneralize_ThenShouldReturnNull()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "first3" }
            };

            var node = CreateStringNode("");
            var result = _processor.Process(node, null, settings);

            Assert.Null(result);
        }

        [Fact]
        public void GivenNullPostalCode_WhenGeneralize_ThenShouldReturnNull()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "first3" }
            };

            var node = CreateStringNode(null);
            var result = _processor.Process(node, null, settings);

            Assert.Null(result);
        }

        private ElementNode CreateStringNode(string value)
        {
            if (value == null)
            {
                return null;
            }
            var fhirString = new FhirString(value);
            return ElementNode.FromElement(fhirString.ToTypedElement());
        }
    }
}
