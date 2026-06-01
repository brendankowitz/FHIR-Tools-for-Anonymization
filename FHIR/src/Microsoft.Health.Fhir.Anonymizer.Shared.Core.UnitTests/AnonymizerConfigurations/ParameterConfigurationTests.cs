using System.Security;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    /// <summary>
    /// Tests for the <see cref="ParameterConfiguration"/> data model.
    /// Validation behaviour is covered in depth by <see cref="ParameterConfigurationValidatorTests"/>.
    /// This class retains tests that call <see cref="ParameterConfigurationValidator.Validate"/> so
    /// that historical test coverage is preserved while call sites are updated to use the
    /// dedicated validator.
    /// </summary>
    public class ParameterConfigurationTests
    {
        // -----------------------------------------------------------------------
        // DateShiftFixedOffsetInDays - valid cases (should NOT throw)
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsNull_DoesNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = null
            };

            ParameterConfigurationValidator.Validate(config);
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsZero_DoesNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = 0
            };

            ParameterConfigurationValidator.Validate(config);
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsAtMinBoundary_DoesNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = ParameterDefaults.MinDateShiftOffsetDays // -365
            };

            ParameterConfigurationValidator.Validate(config);
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsAtMaxBoundary_DoesNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = ParameterDefaults.MaxDateShiftOffsetDays // +365
            };

            ParameterConfigurationValidator.Validate(config);
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

            ParameterConfigurationValidator.Validate(config);
        }

        // -----------------------------------------------------------------------
        // DateShiftFixedOffsetInDays - invalid cases (should throw)
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsBelowMin_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = ParameterDefaults.MinDateShiftOffsetDays - 1 // -366
            };

            var ex = Assert.Throws<AnonymizerConfigurationException>(() => ParameterConfigurationValidator.Validate(config));
            Assert.Contains("-366", ex.Message);
            Assert.Contains(ParameterDefaults.MinDateShiftOffsetDays.ToString(), ex.Message);
            Assert.Contains(ParameterDefaults.MaxDateShiftOffsetDays.ToString(), ex.Message);
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsAboveMax_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = ParameterDefaults.MaxDateShiftOffsetDays + 1 // +366
            };

            var ex = Assert.Throws<AnonymizerConfigurationException>(() => ParameterConfigurationValidator.Validate(config));
            Assert.Contains("366", ex.Message);
            Assert.Contains(ParameterDefaults.MinDateShiftOffsetDays.ToString(), ex.Message);
            Assert.Contains(ParameterDefaults.MaxDateShiftOffsetDays.ToString(), ex.Message);
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

            var ex = Assert.Throws<AnonymizerConfigurationException>(() => ParameterConfigurationValidator.Validate(config));
            Assert.Contains(offset.ToString(), ex.Message);
            Assert.Contains(ParameterDefaults.MinDateShiftOffsetDays.ToString(), ex.Message);
            Assert.Contains(ParameterDefaults.MaxDateShiftOffsetDays.ToString(), ex.Message);
        }

        // -----------------------------------------------------------------------
        // Constants sanity checks
        // -----------------------------------------------------------------------

        [Fact]
        public void Constants_MinAndMaxDateShiftOffset_HaveExpectedValues()
        {
            Assert.Equal(-365, ParameterDefaults.MinDateShiftOffsetDays);
            Assert.Equal(365, ParameterDefaults.MaxDateShiftOffsetDays);
        }

        [Fact]
        public void Constants_MinCryptoHashKeyLength_HasExpectedValue()
        {
            Assert.Equal(32, ParameterDefaults.MinCryptoHashKeyLength);
        }

        // -----------------------------------------------------------------------
        // CryptoHashKey - whitespace-only (should throw SecurityException)
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

            Assert.Throws<SecurityException>(() => ParameterConfigurationValidator.Validate(config));
        }

        // -----------------------------------------------------------------------
        // CryptoHashKey - below minimum length (should throw SecurityException)
        // -----------------------------------------------------------------------

        [Fact]
        public void TestValidate_CryptoHashKey_BelowMinimum_ThrowsSecurityException()
        {
            // -3 + 2 chars = MinLength - 1; mixed chars avoid the weak-key pattern check
            var shortKey = new string('a', ParameterDefaults.MinCryptoHashKeyLength - 3) + "bc";
            Assert.Equal(ParameterDefaults.MinCryptoHashKeyLength - 1, shortKey.Length);

            var config = new ParameterConfiguration
            {
                CryptoHashKey = shortKey
            };

            var ex = Assert.Throws<SecurityException>(() => ParameterConfigurationValidator.Validate(config));
            Assert.Contains((ParameterDefaults.MinCryptoHashKeyLength - 1).ToString(), ex.Message);
            Assert.Contains(ParameterDefaults.MinCryptoHashKeyLength.ToString(), ex.Message);
        }

        // -----------------------------------------------------------------------
        // CryptoHashKey - at minimum length (should NOT throw)
        // -----------------------------------------------------------------------

        [Fact]
        public void TestValidate_CryptoHashKey_AtMinimum_DoesNotThrow()
        {
            const string thirtyTwoCharKey = "abcdefghijklmnopqrstuvwxyz123456"; // 32 chars
            Assert.Equal(ParameterDefaults.MinCryptoHashKeyLength, thirtyTwoCharKey.Length);

            var config = new ParameterConfiguration
            {
                CryptoHashKey = thirtyTwoCharKey
            };

            ParameterConfigurationValidator.Validate(config);
        }

        // -----------------------------------------------------------------------
        // CryptoHashKey - above minimum length (should NOT throw)
        // -----------------------------------------------------------------------

        [Fact]
        public void TestValidate_CryptoHashKey_AboveMinimum_DoesNotThrow()
        {
            const string fortyCharKey = "abcdefghijklmnopqrstuvwxyz1234567890abcd"; // 40 chars
            Assert.Equal(40, fortyCharKey.Length);

            var config = new ParameterConfiguration
            {
                CryptoHashKey = fortyCharKey
            };

            ParameterConfigurationValidator.Validate(config);
        }

        // -----------------------------------------------------------------------
        // DateShiftKey + DateShiftScope validation
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_ResourceScopeWithEmptyDateShiftKeyAndNoFixedOffset_DoesNotThrow()
        {
            // Resource scope (default) does not require a dateShiftKey. Configurations that
            // do not use date shifting must continue to pass validation without specifying one.
            var config = new ParameterConfiguration
            {
                DateShiftScope = DateShiftScope.Resource,
                DateShiftKey = string.Empty,
                DateShiftFixedOffsetInDays = null
            };

            ParameterConfigurationValidator.Validate(config);
        }

        [Fact]
        public void Validate_ResourceScopeWithNullDateShiftKeyAndNoFixedOffset_DoesNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftScope = DateShiftScope.Resource,
                DateShiftKey = null,
                DateShiftFixedOffsetInDays = null
            };

            ParameterConfigurationValidator.Validate(config);
        }

        [Fact]
        public void Validate_ResourceScopeWithValidDateShiftKey_DoesNotThrow()
        {
            const string validKey = "abcdefghijklmnopqrstuvwxyz123456"; // 32 chars
            var config = new ParameterConfiguration
            {
                DateShiftScope = DateShiftScope.Resource,
                DateShiftKey = validKey,
                DateShiftFixedOffsetInDays = null
            };

            ParameterConfigurationValidator.Validate(config);
        }

        [Fact]
        public void Validate_FileScopeWithEmptyDateShiftKeyAndNoFixedOffset_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DateShiftScope = DateShiftScope.File,
                DateShiftKey = string.Empty,
                DateShiftFixedOffsetInDays = null
            };

            var ex = Assert.Throws<AnonymizerConfigurationException>(() => ParameterConfigurationValidator.Validate(config));
            Assert.Contains("dateShiftKey", ex.Message);
        }

        [Fact]
        public void Validate_FolderScopeWithNullDateShiftKeyAndNoFixedOffset_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DateShiftScope = DateShiftScope.Folder,
                DateShiftKey = null,
                DateShiftFixedOffsetInDays = null
            };

            var ex = Assert.Throws<AnonymizerConfigurationException>(() => ParameterConfigurationValidator.Validate(config));
            Assert.Contains("dateShiftKey", ex.Message);
        }

        // -----------------------------------------------------------------------
        // K-anonymity settings validation
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_KAnonymity_ValidSettings_DoesNotThrow()
        {
            var config = new ParameterConfiguration
            {
                KAnonymitySettings = new KAnonymityParameterConfiguration
                {
                    KValue = 5,
                    SuppressionThreshold = 0.3
                }
            };

            ParameterConfigurationValidator.Validate(config);
        }

        [Fact]
        public void Validate_KAnonymity_KValueOne_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                KAnonymitySettings = new KAnonymityParameterConfiguration
                {
                    KValue = 1
                }
            };

            Assert.Throws<AnonymizerConfigurationException>(() => ParameterConfigurationValidator.Validate(config));
        }

        [Fact]
        public void Validate_KAnonymity_SuppressionThresholdNegative_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                KAnonymitySettings = new KAnonymityParameterConfiguration
                {
                    KValue = 5,
                    SuppressionThreshold = -0.1
                }
            };

            Assert.Throws<AnonymizerConfigurationException>(() => ParameterConfigurationValidator.Validate(config));
        }

        [Fact]
        public void Validate_KAnonymity_SuppressionThresholdExceedsOne_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                KAnonymitySettings = new KAnonymityParameterConfiguration
                {
                    KValue = 5,
                    SuppressionThreshold = 1.1
                }
            };

            Assert.Throws<AnonymizerConfigurationException>(() => ParameterConfigurationValidator.Validate(config));
        }
    }
}
