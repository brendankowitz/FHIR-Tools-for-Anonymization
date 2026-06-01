using System.Security;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    /// <summary>
    /// Focused unit tests for <see cref="ParameterConfigurationValidator"/>.
    /// Tests the validator in isolation from the data model construction concerns.
    /// </summary>
    public class ParameterConfigurationValidatorTests
    {
        // -----------------------------------------------------------------------
        // Null config guard
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_WhenConfigIsNull_DoesNotThrow()
        {
            // A null ParameterConfiguration is explicitly valid — it means no global
            // parameters are configured and all defaults apply.
            ParameterConfigurationValidator.Validate(null);
        }

        // -----------------------------------------------------------------------
        // DateShiftFixedOffsetInDays — valid boundary and range cases
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsNull_DoesNotThrow()
        {
            // null means use key-based shift; no offset range validation is needed
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
        // DateShiftFixedOffsetInDays — invalid cases
        // -----------------------------------------------------------------------

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
        }

        // -----------------------------------------------------------------------
        // DateShiftKey + DateShiftScope validation
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_ResourceScope_WithNullDateShiftKeyAndNoFixedOffset_DoesNotThrow()
        {
            // Resource scope is the default. A missing key does NOT require a key because
            // each resource derives its own offset independently; cross-resource consistency
            // is not required. Existing configurations that do not use date-shifting at all
            // must continue to pass validation without specifying a dateShiftKey.
            var config = new ParameterConfiguration
            {
                DateShiftScope = DateShiftScope.Resource,
                DateShiftKey = null,
                DateShiftFixedOffsetInDays = null
            };

            ParameterConfigurationValidator.Validate(config);
        }

        [Fact]
        public void Validate_ResourceScope_WithEmptyDateShiftKeyAndNoFixedOffset_DoesNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftScope = DateShiftScope.Resource,
                DateShiftKey = string.Empty,
                DateShiftFixedOffsetInDays = null
            };

            ParameterConfigurationValidator.Validate(config);
        }

        [Fact]
        public void Validate_ResourceScope_WithValidDateShiftKey_DoesNotThrow()
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
            // File scope requires a key so all resources in the same file share the same offset.
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
            // Folder scope requires a key so all resources in the same folder share the same offset.
            var config = new ParameterConfiguration
            {
                DateShiftScope = DateShiftScope.Folder,
                DateShiftKey = null,
                DateShiftFixedOffsetInDays = null
            };

            var ex = Assert.Throws<AnonymizerConfigurationException>(() => ParameterConfigurationValidator.Validate(config));
            Assert.Contains("dateShiftKey", ex.Message);
        }

        [Fact]
        public void Validate_FileScopeWithNullKeyButFixedOffsetSet_DoesNotThrow()
        {
            // Fixed offset bypasses the key requirement even for File scope.
            var config = new ParameterConfiguration
            {
                DateShiftScope = DateShiftScope.File,
                DateShiftKey = null,
                DateShiftFixedOffsetInDays = 30
            };

            ParameterConfigurationValidator.Validate(config);
        }

        [Fact]
        public void Validate_FolderScopeWithValidKey_DoesNotThrow()
        {
            const string validKey = "abcdefghijklmnopqrstuvwxyz123456";
            var config = new ParameterConfiguration
            {
                DateShiftScope = DateShiftScope.Folder,
                DateShiftKey = validKey,
                DateShiftFixedOffsetInDays = null
            };

            ParameterConfigurationValidator.Validate(config);
        }

        // -----------------------------------------------------------------------
        // CryptoHashKey — whitespace-only (SecurityException)
        // -----------------------------------------------------------------------

        [Theory]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("   ")]
        [InlineData(" \t \n ")]
        public void Validate_CryptoHashKey_WhitespaceOnly_ThrowsSecurityException(string key)
        {
            var config = new ParameterConfiguration { CryptoHashKey = key };
            Assert.Throws<SecurityException>(() => ParameterConfigurationValidator.Validate(config));
        }

        // -----------------------------------------------------------------------
        // CryptoHashKey — below minimum length (SecurityException)
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_CryptoHashKey_BelowMinimumLength_ThrowsSecurityException()
        {
            // Construct a key that is one character below the minimum length.
            // Use mixed characters to avoid triggering the weak-key pattern check.
            var shortKey = new string('a', ParameterDefaults.MinCryptoHashKeyLength - 3) + "bc";
            Assert.Equal(ParameterDefaults.MinCryptoHashKeyLength - 1, shortKey.Length);

            var config = new ParameterConfiguration { CryptoHashKey = shortKey };

            var ex = Assert.Throws<SecurityException>(() => ParameterConfigurationValidator.Validate(config));
            Assert.Contains((ParameterDefaults.MinCryptoHashKeyLength - 1).ToString(), ex.Message);
            Assert.Contains(ParameterDefaults.MinCryptoHashKeyLength.ToString(), ex.Message);
        }

        // -----------------------------------------------------------------------
        // CryptoHashKey — at and above minimum length (no throw)
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_CryptoHashKey_AtMinimumLength_DoesNotThrow()
        {
            const string thirtyTwoCharKey = "abcdefghijklmnopqrstuvwxyz123456"; // exactly 32 chars
            Assert.Equal(ParameterDefaults.MinCryptoHashKeyLength, thirtyTwoCharKey.Length);

            var config = new ParameterConfiguration { CryptoHashKey = thirtyTwoCharKey };
            ParameterConfigurationValidator.Validate(config);
        }

        [Fact]
        public void Validate_CryptoHashKey_AboveMinimumLength_DoesNotThrow()
        {
            const string longKey = "abcdefghijklmnopqrstuvwxyz1234567890abcd"; // 40 chars

            var config = new ParameterConfiguration { CryptoHashKey = longKey };
            ParameterConfigurationValidator.Validate(config);
        }

        // -----------------------------------------------------------------------
        // EncryptKey — invalid AES key size (AnonymizerConfigurationException)
        // -----------------------------------------------------------------------

        [Theory]
        [InlineData("tooshort")]          // 8 bytes = 64 bits — invalid
        [InlineData("exactly17byteskey")]  // 17 bytes = 136 bits — invalid
        public void Validate_EncryptKey_InvalidAesKeySize_ThrowsAnonymizerConfigurationException(string key)
        {
            var config = new ParameterConfiguration { EncryptKey = key };
            var ex = Assert.Throws<AnonymizerConfigurationException>(() => ParameterConfigurationValidator.Validate(config));
            Assert.Contains("bits", ex.Message);
        }

        // -----------------------------------------------------------------------
        // EncryptKey — valid AES key sizes (no throw)
        // -----------------------------------------------------------------------

        [Theory]
        [InlineData("a3f8b2e1d4c7f9a0")]                 // 16 bytes => AES-128
        [InlineData("a3f8b2e1d4c7f9a0b3e2d5c8")]         // 24 bytes => AES-192
        [InlineData("a3f8b2e1d4c7f9a0b3e2d5c8f1a4b7e0")] // 32 bytes => AES-256
        public void Validate_EncryptKey_ValidAesKeySize_DoesNotThrow(string key)
        {
            var config = new ParameterConfiguration { EncryptKey = key };
            ParameterConfigurationValidator.Validate(config);
        }

        // -----------------------------------------------------------------------
        // TODO / FIXME placeholder detection (SecurityException)
        // -----------------------------------------------------------------------

        [Theory]
        [InlineData("TODO")]
        [InlineData("todo")]
        [InlineData("Todo")]
        [InlineData("FIXME")]
        [InlineData("fixme")]
        [InlineData("Fixme")]
        [InlineData("todolist_key_that_is_long_enough_for_length_check")]
        public void Validate_CryptoHashKey_ContainsPlaceholder_ThrowsSecurityException(string key)
        {
            var config = new ParameterConfiguration { CryptoHashKey = key };
            Assert.Throws<SecurityException>(() => ParameterConfigurationValidator.Validate(config));
        }

        [Theory]
        [InlineData("TODO")]
        [InlineData("todo")]
        [InlineData("FIXME")]
        [InlineData("fixme_embedded_key_that_is_long_enough_for_check")]
        public void Validate_EncryptKey_ContainsPlaceholder_ThrowsSecurityException(string key)
        {
            var config = new ParameterConfiguration { EncryptKey = key };
            Assert.Throws<SecurityException>(() => ParameterConfigurationValidator.Validate(config));
        }

        [Theory]
        [InlineData("TODO")]
        [InlineData("todo")]
        [InlineData("FIXME")]
        [InlineData("fixme_embedded_key_that_is_long_enough_for_check")]
        public void Validate_DateShiftKey_ContainsPlaceholder_ThrowsSecurityException(string key)
        {
            var config = new ParameterConfiguration
            {
                DateShiftKey = key,
                DateShiftFixedOffsetInDays = 0
            };
            Assert.Throws<SecurityException>(() => ParameterConfigurationValidator.Validate(config));
        }

        // -----------------------------------------------------------------------
        // Valid keys — guard against over-eager pattern matching
        // -----------------------------------------------------------------------

        [Theory]
        [InlineData("a3f8b2e1d4c7f9a0b3e2d5c8f1a4b7e0")] // 32 hex chars, no dangerous patterns
        public void Validate_ValidCryptoHashKey_DoesNotThrow(string key)
        {
            var config = new ParameterConfiguration { CryptoHashKey = key };
            ParameterConfigurationValidator.Validate(config);
        }

        // -----------------------------------------------------------------------
        // Differential privacy settings
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_DifferentialPrivacy_ValidSettings_DoesNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DifferentialPrivacySettings = new DifferentialPrivacyParameterConfiguration
                {
                    Epsilon = 0.5,
                    Delta = 1e-5,
                    Sensitivity = 1.0,
                    MaxCumulativeEpsilon = 5.0
                }
            };

            ParameterConfigurationValidator.Validate(config);
        }

        [Fact]
        public void Validate_DifferentialPrivacy_EpsilonZero_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DifferentialPrivacySettings = new DifferentialPrivacyParameterConfiguration
                {
                    Epsilon = 0
                }
            };

            Assert.Throws<AnonymizerConfigurationException>(() => ParameterConfigurationValidator.Validate(config));
        }

        [Fact]
        public void Validate_DifferentialPrivacy_EpsilonNegative_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DifferentialPrivacySettings = new DifferentialPrivacyParameterConfiguration
                {
                    Epsilon = -0.1
                }
            };

            Assert.Throws<AnonymizerConfigurationException>(() => ParameterConfigurationValidator.Validate(config));
        }

        [Fact]
        public void Validate_DifferentialPrivacy_EpsilonExceedsMax_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DifferentialPrivacySettings = new DifferentialPrivacyParameterConfiguration
                {
                    Epsilon = 10.1,
                    MaxCumulativeEpsilon = 100.0
                }
            };

            Assert.Throws<AnonymizerConfigurationException>(() => ParameterConfigurationValidator.Validate(config));
        }

        [Fact]
        public void Validate_DifferentialPrivacy_DeltaNegative_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DifferentialPrivacySettings = new DifferentialPrivacyParameterConfiguration
                {
                    Epsilon = 1.0,
                    Delta = -0.01
                }
            };

            Assert.Throws<AnonymizerConfigurationException>(() => ParameterConfigurationValidator.Validate(config));
        }

        [Fact]
        public void Validate_DifferentialPrivacy_SensitivityZero_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DifferentialPrivacySettings = new DifferentialPrivacyParameterConfiguration
                {
                    Epsilon = 1.0,
                    Sensitivity = 0
                }
            };

            Assert.Throws<AnonymizerConfigurationException>(() => ParameterConfigurationValidator.Validate(config));
        }

        [Fact]
        public void Validate_DifferentialPrivacy_MaxCumulativeEpsilonZero_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DifferentialPrivacySettings = new DifferentialPrivacyParameterConfiguration
                {
                    Epsilon = 1.0,
                    MaxCumulativeEpsilon = 0
                }
            };

            Assert.Throws<AnonymizerConfigurationException>(() => ParameterConfigurationValidator.Validate(config));
        }

        // -----------------------------------------------------------------------
        // K-anonymity settings
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

            var ex = Assert.Throws<AnonymizerConfigurationException>(() => ParameterConfigurationValidator.Validate(config));
            Assert.Contains("k-value", ex.Message, System.StringComparison.OrdinalIgnoreCase);
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
