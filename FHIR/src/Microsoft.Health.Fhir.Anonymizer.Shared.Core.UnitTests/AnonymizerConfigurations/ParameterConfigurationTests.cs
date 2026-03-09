using System.Security;
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

        [Fact]
        public void Constants_MinCryptoHashKeyLength_HasExpectedValue()
        {
            Assert.Equal(32, ParameterConfiguration.MinCryptoHashKeyLength);
        }

        // -----------------------------------------------------------------------
        // CryptoHashKey — whitespace-only (should throw SecurityException)
        // -----------------------------------------------------------------------

        [Theory]
        [InlineData(" ")]       // single space
        [InlineData("\t")]      // tab
        [InlineData("   ")]     // multiple spaces
        [InlineData(" \t \n ")] // mixed whitespace
        public void TestValidate_CryptoHashKey_WhitespaceOnly_ThrowsSecurityException(string key)
        {
            var config = new ParameterConfiguration
            {
                CryptoHashKey = key
            };

            Assert.Throws<SecurityException>(() => config.Validate());
        }

        // -----------------------------------------------------------------------
        // CryptoHashKey — below minimum length (should throw SecurityException)
        // -----------------------------------------------------------------------

        [Fact]
        public void TestValidate_CryptoHashKey_BelowMinimum_ThrowsSecurityException()
        {
            // 31 distinct characters — passes the placeholder and weak-key checks but
            // fails the hard 32-character minimum length requirement.
            // NOTE: a short all-same-character key (e.g. "aaa...") would be caught by
            // the weak-key check (all-same-char pattern) before reaching the length check.
            const string thirtyOneCharKey = "abcdefghijklmnopqrstuvwxyz12345"; // 31 chars
            Assert.Equal(31, thirtyOneCharKey.Length);

            var config = new ParameterConfiguration
            {
                CryptoHashKey = thirtyOneCharKey
            };

            var ex = Assert.Throws<SecurityException>(() => config.Validate());
            Assert.Contains("31", ex.Message);
            Assert.Contains("32", ex.Message);
        }

        // -----------------------------------------------------------------------
        // CryptoHashKey — at minimum length (should NOT throw)
        // -----------------------------------------------------------------------

        [Fact]
        public void TestValidate_CryptoHashKey_AtMinimum_DoesNotThrow()
        {
            // Exactly 32 characters composed of distinct characters to avoid weak-key detection.
            const string thirtyTwoCharKey = "abcdefghijklmnopqrstuvwxyz123456"; // 32 chars
            Assert.Equal(32, thirtyTwoCharKey.Length);

            var config = new ParameterConfiguration
            {
                CryptoHashKey = thirtyTwoCharKey
            };

            // Should not throw — exactly meets the minimum length requirement.
            config.Validate();
        }

        // -----------------------------------------------------------------------
        // CryptoHashKey — above minimum length (should NOT throw)
        // -----------------------------------------------------------------------

        [Fact]
        public void TestValidate_CryptoHashKey_AboveMinimum_DoesNotThrow()
        {
            // 40 characters — comfortably above the 32-character minimum.
            const string fortyCharKey = "abcdefghijklmnopqrstuvwxyz1234567890abcd"; // 40 chars
            Assert.Equal(40, fortyCharKey.Length);

            var config = new ParameterConfiguration
            {
                CryptoHashKey = fortyCharKey
            };

            // Should not throw — exceeds the minimum length requirement.
            config.Validate();
        }
    }
}
