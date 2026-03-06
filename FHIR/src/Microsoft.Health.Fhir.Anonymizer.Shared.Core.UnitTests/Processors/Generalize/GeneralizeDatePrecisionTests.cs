using System;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors.Generalize
{
    /// <summary>
    /// Unit tests for date precision reduction functionality.
    /// Tests cover reducing date/datetime precision to year, month, or day levels.
    /// </summary>
    public class GeneralizeDatePrecisionTests
    {
        private readonly GeneralizeProcessor _processor;

        public GeneralizeDatePrecisionTests()
        {
            _processor = new GeneralizeProcessor();
        }

        [Fact]
        public void GivenFullDate_WhenGeneralizeToYear_ThenShouldReturnYearOnly()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "year" }
            };

            var node = CreateDateNode("2020-05-15");
            var result = _processor.Process(node, null, settings);

            Assert.NotNull(result);
            Assert.Equal("2020", result.Value.ToString());
        }

        [Fact]
        public void GivenFullDate_WhenGeneralizeToYearMonth_ThenShouldReturnYearMonth()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "year-month" }
            };

            var node = CreateDateNode("2020-05-15");
            var result = _processor.Process(node, null, settings);

            Assert.NotNull(result);
            Assert.Equal("2020-05", result.Value.ToString());
        }

        [Fact]
        public void GivenDateTime_WhenGeneralizeToDate_ThenShouldReturnDateOnly()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "date" }
            };

            var node = CreateDateTimeNode("2020-05-15T14:30:25Z");
            var result = _processor.Process(node, null, settings);

            Assert.NotNull(result);
            Assert.Equal("2020-05-15", result.Value.ToString());
        }

        [Fact]
        public void GivenDateTime_WhenGeneralizeToYear_ThenShouldReturnYearOnly()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "year" }
            };

            var node = CreateDateTimeNode("2020-05-15T14:30:25Z");
            var result = _processor.Process(node, null, settings);

            Assert.NotNull(result);
            Assert.Equal("2020", result.Value.ToString());
        }

        [Fact]
        public void GivenDateWithYearMonthPrecision_WhenGeneralizeToYear_ThenShouldReturnYear()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "year" }
            };

            var node = CreateDateNode("2020-05");
            var result = _processor.Process(node, null, settings);

            Assert.NotNull(result);
            Assert.Equal("2020", result.Value.ToString());
        }

        [Fact]
        public void GivenDateWithYearPrecision_WhenGeneralizeToYear_ThenShouldReturnSame()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "year" }
            };

            var node = CreateDateNode("2020");
            var result = _processor.Process(node, null, settings);

            Assert.NotNull(result);
            Assert.Equal("2020", result.Value.ToString());
        }

        [Fact]
        public void GivenInstant_WhenGeneralizeToDate_ThenShouldReturnDate()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "date" }
            };

            var node = CreateInstantNode("2020-05-15T14:30:25.123Z");
            var result = _processor.Process(node, null, settings);

            Assert.NotNull(result);
            Assert.Equal("2020-05-15", result.Value.ToString());
        }

        private ElementNode CreateDateNode(string dateString)
        {
            var date = new Date(dateString);
            return ElementNode.FromElement(date.ToTypedElement());
        }

        private ElementNode CreateDateTimeNode(string dateTimeString)
        {
            var dateTime = new FhirDateTime(dateTimeString);
            return ElementNode.FromElement(dateTime.ToTypedElement());
        }

        private ElementNode CreateInstantNode(string instantString)
        {
            var instant = new Instant(DateTimeOffset.Parse(instantString));
            return ElementNode.FromElement(instant.ToTypedElement());
        }
    }
}
