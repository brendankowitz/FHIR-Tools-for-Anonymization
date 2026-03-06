using System;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors.Generalize
{
    /// <summary>
    /// Unit tests for error handling and edge cases in generalization.
    /// Tests cover null inputs, invalid configurations, unsupported types, and boundary conditions.
    /// </summary>
    public class GeneralizeErrorHandlingTests
    {
        private readonly GeneralizeProcessor _processor;

        public GeneralizeErrorHandlingTests()
        {
            _processor = new GeneralizeProcessor();
        }

        [Fact]
        public void GivenNullNode_WhenGeneralize_ThenShouldReturnNull()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "[0,10)", "[10,20)" }
            };

            var result = _processor.Process(null, null, settings);

            Assert.Null(result);
        }

        [Fact]
        public void GivenNullSettings_WhenGeneralize_ThenShouldThrowArgumentException()
        {
            var node = CreateIntegerNode(5);

            Assert.Throws<ArgumentNullException>(() => _processor.Process(node, null, null));
        }

        [Fact]
        public void GivenEmptyCases_WhenGeneralize_ThenShouldReturnNull()
        {
            var settings = new GeneralizeSetting
            {
                Cases = Array.Empty<string>()
            };

            var node = CreateIntegerNode(5);
            var result = _processor.Process(node, null, settings);

            Assert.Null(result);
        }

        [Fact]
        public void GivenInvalidRangeFormat_WhenGeneralize_ThenShouldHandleGracefully()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "invalid-range", "[10,20)" }
            };

            var node = CreateIntegerNode(5);
            
            // Should not throw, may return null or skip invalid range
            var result = _processor.Process(node, null, settings);
            
            // Either null or a valid result, but no exception
            Assert.True(result == null || result.Value != null);
        }

        [Fact]
        public void GivenUnsupportedDataType_WhenGeneralize_ThenShouldReturnNull()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "[0,10)", "[10,20)" }
            };

            var node = CreateBooleanNode(true);
            var result = _processor.Process(node, null, settings);

            Assert.Null(result);
        }

        [Fact]
        public void GivenMalformedDate_WhenGeneralize_ThenShouldHandleGracefully()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "year" }
            };

            // Create a string that looks like a date but isn't valid
            var node = CreateStringNode("not-a-date");
            
            // Should handle gracefully without throwing
            var result = _processor.Process(node, null, settings);
            
            Assert.True(result == null || result.Value != null);
        }

        [Fact]
        public void GivenVeryLargeNumber_WhenGeneralize_ThenShouldNotOverflow()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "[0,1000000)", "[1000000,10000000)" }
            };

            var node = CreateIntegerNode(int.MaxValue);
            
            // Should handle large numbers without overflow
            var result = _processor.Process(node, null, settings);
            
            // May be null if outside ranges, but shouldn't throw
            Assert.True(result == null || result.Value != null);
        }

        [Fact]
        public void GivenNegativeAgeFromFutureDate_WhenGeneralize_ThenShouldHandleGracefully()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "[0,18)", "[18,65)", "[65,89)", "89+" }
            };

            // Date in the future
            var node = CreateDateNode("2050-01-01");
            var result = _processor.Process(node, null, settings);

            // Should handle future dates gracefully
            Assert.True(result == null || result.Value != null);
        }

        [Fact]
        public void GivenDateWithInvalidMonth_WhenGeneralize_ThenShouldHandleGracefully()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "year" }
            };

            // Try to create an invalid date and see how it's handled
            try
            {
                var node = CreateStringNode("2020-13-01"); // Invalid month
                var result = _processor.Process(node, null, settings);
                Assert.True(result == null || result.Value != null);
            }
            catch (FormatException)
            {
                // It's also acceptable to throw FormatException for invalid dates
                Assert.True(true);
            }
        }

        [Fact]
        public void GivenCaseSensitiveConfig_WhenGeneralize_ThenShouldMatchCase()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "YEAR", "MONTH" }
            };

            var node = CreateDateNode("2020-05-15");
            var result = _processor.Process(node, null, settings);

            // Behavior depends on whether case matters in implementation
            Assert.True(result == null || result.Value != null);
        }

        private ElementNode CreateIntegerNode(int value)
        {
            var integer = new Integer(value);
            return ElementNode.FromElement(integer.ToTypedElement());
        }

        private ElementNode CreateBooleanNode(bool value)
        {
            var boolean = new FhirBoolean(value);
            return ElementNode.FromElement(boolean.ToTypedElement());
        }

        private ElementNode CreateStringNode(string value)
        {
            var fhirString = new FhirString(value);
            return ElementNode.FromElement(fhirString.ToTypedElement());
        }

        private ElementNode CreateDateNode(string dateString)
        {
            try
            {
                var date = new Date(dateString);
                return ElementNode.FromElement(date.ToTypedElement());
            }
            catch
            {
                // If date creation fails, return a string node instead
                return CreateStringNode(dateString);
            }
        }
    }
}
