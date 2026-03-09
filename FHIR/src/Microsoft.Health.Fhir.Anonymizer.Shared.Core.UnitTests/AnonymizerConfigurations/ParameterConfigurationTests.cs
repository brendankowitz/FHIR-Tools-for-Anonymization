using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    public class ParameterConfigurationTests
    {
        // -----------------------------------------------------------------------
        // DateShiftFixedOffsetInDays — valid cases (should NOT throw)
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsNull_DoesNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = null
            };

            // Should not throw — null means "use key-based shift"
            config.Validate();
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsZero_DoesNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = 0
            };

            config.Validate();
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsAtMinBoundary_DoesNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = ParameterConfiguration.MinDateShiftOffsetDays // -365
            };

            config.Validate();
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsAtMaxBoundary_DoesNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = ParameterConfiguration.MaxDateShiftOffsetDays // +365
            };

            config.Validate();
        }

        [Theory]
        [InlineData(-364)]
        [InlineData(-1)]
        [InlineData(1)]
        [InlineData(364)]
        public void Validate_WhenDateShiftFixedOffsetIsWithinRange_DoesNotThrow(int offset)
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = offset
            };

            config.Validate();
        }

        // -----------------------------------------------------------------------
        // DateShiftFixedOffsetInDays — invalid cases (should throw)
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsBelowMin_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = ParameterConfiguration.MinDateShiftOffsetDays - 1 // -366
            };

            var ex = Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
            Assert.Contains("-366", ex.Message);
            Assert.Contains("-365", ex.Message);
            Assert.Contains("365", ex.Message);
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsAboveMax_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = ParameterConfiguration.MaxDateShiftOffsetDays + 1 // +366
            };

            var ex = Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
            Assert.Contains("366", ex.Message);
            Assert.Contains("-365", ex.Message);
            Assert.Contains("365", ex.Message);
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsLargeNegative_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = int.MinValue
            };

            var ex = Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
            Assert.Contains(int.MinValue.ToString(), ex.Message);
            Assert.Contains("-365", ex.Message);
            Assert.Contains("365", ex.Message);
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsLargePositive_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = int.MaxValue
            };

            var ex = Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
            Assert.Contains(int.MaxValue.ToString(), ex.Message);
            Assert.Contains("-365", ex.Message);
            Assert.Contains("365", ex.Message);
        }

        [Theory]
        [InlineData(-366)]
        [InlineData(-1000)]
        [InlineData(366)]
        [InlineData(1000)]
        public void Validate_WhenDateShiftFixedOffsetIsOutOfRange_ThrowsAnonymizerConfigurationException(int offset)
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = offset
            };

            var ex = Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
            Assert.Contains(offset.ToString(), ex.Message);
            Assert.Contains("-365", ex.Message);
            Assert.Contains("365", ex.Message);
        }

        // -----------------------------------------------------------------------
        // Constants sanity checks
        // -----------------------------------------------------------------------

        [Fact]
        public void Constants_MinAndMaxDateShiftOffset_HaveExpectedValues()
        {
            Assert.Equal(-365, ParameterConfiguration.MinDateShiftOffsetDays);
            Assert.Equal(365, ParameterConfiguration.MaxDateShiftOffsetDays);
        }
    }
}
