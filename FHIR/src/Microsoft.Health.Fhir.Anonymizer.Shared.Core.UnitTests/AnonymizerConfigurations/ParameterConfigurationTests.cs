using System.Security;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    public class ParameterConfigurationTests
    {
        // -----------------------------------------------------------------------
        // DateShiftFixedOffsetInDays validation tests
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsNull_DoesNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = null
            };
            config.Validate(); // null is always valid; key-based shift will be used
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
                DateShiftFixedOffsetInDays = ParameterConfiguration.MinDateShiftOffsetDays
            };
            config.Validate();
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsAtMaxBoundary_DoesNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = ParameterConfiguration.MaxDateShiftOffsetDays
            };
            config.Validate();
        }

        [Theory]
        [InlineData(-100)]
        [InlineData(-1)]
        [InlineData(1)]
        [InlineData(100)]
        public void Validate_WhenDateShiftFixedOffsetIsWithinRange_DoesNotThrow(int offset)
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = offset
            };
            config.Validate();
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsBelowMin_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = ParameterConfiguration.MinDateShiftOffsetDays - 1
            };
            Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsAboveMax_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = ParameterConfiguration.MaxDateShiftOffsetDays + 1
            };
            Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsLargeNegative_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = -1000
            };
            Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsLargePositive_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = 1000
            };
            Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
        }

        [Theory]
        [InlineData(-366)]
        [InlineData(-500)]
        [InlineData(366)]
        [InlineData(500)]
        public void Validate_WhenDateShiftFixedOffsetIsOutOfRange_ThrowsAnonymizerConfigurationException(int offset)
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = offset
            };
            Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
        }

        [Fact]
        public void Constants_MinAndMaxDateShiftOffset_HaveExpectedValues()
        {
            Assert.Equal(-365, ParameterConfiguration.MinDateShiftOffsetDays);
            Assert.Equal(365, ParameterConfiguration.MaxDateShiftOffsetDays);
        }

        // -----------------------------------------------------------------------
        // EncryptKey size validation tests
        // -----------------------------------------------------------------------

        [Fact]
        public void GivenEncryptKeyOf16Bytes_WhenValidate_NoExceptionThrown()
        {
            var config = new ParameterConfiguration
            {
                // 16 distinct ASCII chars = 128 bits, valid AES key size.
                // Must NOT be all-same-character (would trigger the weak-key guard).
                EncryptKey = "abcdefghijklmnop"
            };
            config.Validate(); // should not throw
        }

        [Fact]
        public void GivenEncryptKeyOf24Bytes_WhenValidate_NoExceptionThrown()
        {
            var config = new ParameterConfiguration
            {
                // 24 distinct ASCII chars = 192 bits, valid AES key size.
                EncryptKey = "abcdefghijklmnopqrstuvwx"
            };
            config.Validate(); // should not throw
        }

        [Fact]
        public void GivenEncryptKeyOf32Bytes_WhenValidate_NoExceptionThrown()
        {
            var config = new ParameterConfiguration
            {
                // 32 distinct ASCII chars = 256 bits, valid AES key size.
                EncryptKey = "abcdefghijklmnopqrstuvwxyz012345"
            };
            config.Validate(); // should not throw
        }

        [Fact]
        public void GivenEncryptKeyOf20Bytes_WhenValidate_ExceptionThrown()
        {
            var config = new ParameterConfiguration
            {
                // Same 20-char key used in configuration-invalid-encryptkey.json.
                // 20 bytes = 160 bits, not a valid AES key size.
                // This string uses mixed digits (not all-same-character), so the
                // weak-key check is NOT triggered — only the key-size check fires,
                // giving us a predictable AnonymizerConfigurationException.
                EncryptKey = "01234567890123456789"
            };
            Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
        }

        [Fact]
        public void GivenEncryptKeyOf8Bytes_WhenValidate_ExceptionThrown()
        {
            var config = new ParameterConfiguration
            {
                // 8 distinct ASCII chars = 64 bits, not a valid AES key size.
                // Must NOT be all-same-character (would trigger SecurityException, not
                // AnonymizerConfigurationException, so the Assert.Throws would fail).
                EncryptKey = "abcdefgh"
            };
            Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
        }

        [Fact]
        public void GivenNullEncryptKey_WhenValidate_NoExceptionThrown()
        {
            var config = new ParameterConfiguration
            {
                EncryptKey = null
            };
            config.Validate(); // should not throw
        }

        [Fact]
        public void GivenEmptyEncryptKey_WhenValidate_NoExceptionThrown()
        {
            var config = new ParameterConfiguration
            {
                EncryptKey = string.Empty
            };
            config.Validate(); // should not throw
        }
    }
}
