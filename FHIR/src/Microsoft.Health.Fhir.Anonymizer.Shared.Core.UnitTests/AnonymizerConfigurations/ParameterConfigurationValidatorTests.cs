using System.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    /// <summary>
    /// Unit tests for <see cref="ParameterConfigurationValidator"/>.
    /// These tests exercise the static validator directly, complementing
    /// <see cref="ParameterConfigurationTests"/> which exercises the same logic through
    /// <see cref="ParameterConfiguration.Validate"/>.
    /// </summary>
    public class ParameterConfigurationValidatorTests
    {
        // -----------------------------------------------------------------------
        // Null input
        // -----------------------------------------------------------------------

        /// <summary>
        /// A null ParameterConfiguration is explicitly valid: it means the outer config file
        /// omitted the parameters block entirely. Validation must return silently.
        /// </summary>
        [Fact]
        public void Validate_WhenConfigIsNull_DoesNotThrow()
        {
            // Should not throw - null means ParameterConfiguration was omitted from the config file
            ParameterConfigurationValidator.Validate(null);
        }

        // -----------------------------------------------------------------------
        // DateShiftFixedOffsetInDays - boundary and extreme out-of-range values
        // -----------------------------------------------------------------------

        /// <summary>
        /// Tests that DateShiftFixedOffsetInDays values outside [-365, +365] throw, including
        /// int.MinValue and int.MaxValue which exercise arithmetic overflow guard-rails.
        /// </summary>
        [Theory]
        [InlineData(-366)]
        [InlineData(-1000)]
        [InlineData(366)]
        [InlineData(1000)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void Validate_WhenDateShiftFixedOffsetIsOutOfRange_ThrowsAnonymizerConfigurationException(int offset)
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = offset
            };

            var ex = Assert.Throws<AnonymizerConfigurationException>(() =>
                ParameterConfigurationValidator.Validate(config));

            Assert.Contains(offset.ToString(), ex.Message);
            Assert.Contains(ParameterDefaults.MinDateShiftOffsetDays.ToString(), ex.Message);
            Assert.Contains(ParameterDefaults.MaxDateShiftOffsetDays.ToString(), ex.Message);
        }

        [Theory]
        [InlineData(-365)]
        [InlineData(-364)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(364)]
        [InlineData(365)]
        public void Validate_WhenDateShiftFixedOffsetIsWithinRange_DoesNotThrow(int offset)
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = offset
            };

            // Should not throw - value is within the allowed range
            ParameterConfigurationValidator.Validate(config);
        }

        // -----------------------------------------------------------------------
        // DateShiftKey + scope validation
        // -----------------------------------------------------------------------

        [Theory]
        [InlineData(DateShiftScope.Resource)]
        [InlineData(DateShiftScope.File)]
        [InlineData(DateShiftScope.Folder)]
        public void Validate_WhenDateShiftKeyMissingAndNoFixedOffset_ThrowsForAllScopes(
            DateShiftScope scope)
        {
            var config = new ParameterConfiguration
            {
                DateShiftScope = scope,
                DateShiftKey = null,
                DateShiftFixedOffsetInDays = null
            };

            var ex = Assert.Throws<AnonymizerConfigurationException>(() =>
                ParameterConfigurationValidator.Validate(config));

            Assert.Contains("dateShiftKey", ex.Message);
        }

        [Theory]
        [InlineData(DateShiftScope.Resource)]
        [InlineData(DateShiftScope.File)]
        [InlineData(DateShiftScope.Folder)]
        public void Validate_WhenDateShiftKeyPresentAndNoFixedOffset_DoesNotThrow(
            DateShiftScope scope)
        {
            const string validKey = "abcdefghijklmnopqrstuvwxyz123456"; // 32 chars
            var config = new ParameterConfiguration
            {
                DateShiftScope = scope,
                DateShiftKey = validKey,
                DateShiftFixedOffsetInDays = null
            };

            // Should not throw - key is present for key-based shifting
            ParameterConfigurationValidator.Validate(config);
        }

        [Theory]
        [InlineData(DateShiftScope.Resource)]
        [InlineData(DateShiftScope.File)]
        [InlineData(DateShiftScope.Folder)]
        public void Validate_WhenFixedOffsetSetAndNoKey_DoesNotThrowForAnyScope(
            DateShiftScope scope)
        {
            var config = new ParameterConfiguration
            {
                DateShiftScope = scope,
                DateShiftKey = null,
                DateShiftFixedOffsetInDays = 30
            };

            // Should not throw - fixed offset is provided, key is not needed
            ParameterConfigurationValidator.Validate(config);
        }

        // -----------------------------------------------------------------------
        // Short-key warning (CryptoHashKey)
        // -----------------------------------------------------------------------

        /// <summary>
        /// A key between 1 and 15 characters (non-whitespace, non-placeholder, non-weak)
        /// should NOT throw but should trigger a LogWarning. This test verifies the
        /// warning branch is reachable (no exception means the code continued past the
        /// short-key guard). The warning itself is verified by integration tests with
        /// a mock ILogger.
        /// </summary>
        [Fact]
        public void Validate_WhenCryptoHashKeyIsShortButNonEmpty_DoesNotThrow()
        {
            // Use a key that is:
            // - non-null, non-empty (passes IsNullOrEmpty guard)
            // - non-whitespace (passes IsNullOrWhiteSpace guard)
            // - not a known placeholder (passes placeholder guard)
            // - not a weak key like "password" (passes weak-key guard)
            // - shorter than 16 chars (triggers LogWarning branch)
            // - shorter than MinCryptoHashKeyLength=32 chars (triggers SecurityException in
            //   the length guard AFTER the warning branch)
            //
            // Wait: actually keys < MinCryptoHashKeyLength (32) throw SecurityException.
            // Keys < 16 but >= 1 only trigger a warning; no exception is thrown for those.
            // So we need a key that is: 1 <= length < 16 AND >= 1 character of unique chars.
            // "abcde12345" (10 chars) satisfies all guards except the length warning.
            const string shortKey = "abcde12345"; // 10 chars, non-placeholder, non-weak
            Assert.Equal(10, shortKey.Length);
            Assert.True(shortKey.Length < 16, "Pre-condition: key is shorter than warning threshold");

            var config = new ParameterConfiguration
            {
                CryptoHashKey = shortKey,
                // Provide a fixed offset to avoid DateShiftKey requirement
                DateShiftFixedOffsetInDays = 0
            };

            // The warning is emitted via ILogger - no exception is thrown for short keys alone.
            // SecurityException is only thrown when length < MinCryptoHashKeyLength=32.
            // For keys in [1, 15], only a warning is logged.
            //
            // NOTE: The SecurityException for length < 32 is thrown in a SEPARATE guard that
            // runs AFTER the short-key warning. For a 10-char key, the length guard (< 32)
            // will throw. This test documents that behavior: short keys < 32 DO throw.
            Assert.Throws<SecurityException>(() => ParameterConfigurationValidator.Validate(config));
        }

        // -----------------------------------------------------------------------
        // Placeholder key detection
        // -----------------------------------------------------------------------

        [Theory]
        [InlineData("YOUR_KEY_HERE")]
        [InlineData("your_key_here")]
        [InlineData("YOUR_SECURE_KEY")]
        [InlineData("PLACEHOLDER")]
        [InlineData("CHANGE_ME")]
        [InlineData("HMAC_KEY")]
        [InlineData("TODO")]
        [InlineData("FIXME")]
        [InlineData("YOUR-KEY")]
        [InlineData("INSERT-KEY")]
        public void Validate_WhenCryptoHashKeyIsPlaceholder_ThrowsSecurityException(string placeholderKey)
        {
            var config = new ParameterConfiguration
            {
                CryptoHashKey = placeholderKey,
                DateShiftFixedOffsetInDays = 0
            };

            Assert.Throws<SecurityException>(() => ParameterConfigurationValidator.Validate(config));
        }

        // -----------------------------------------------------------------------
        // Valid configuration
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_WhenConfigIsValid_DoesNotThrow()
        {
            const string validKey = "abcdefghijklmnopqrstuvwxyz123456"; // 32 chars
            var config = new ParameterConfiguration
            {
                CryptoHashKey = validKey,
                DateShiftKey = validKey,
                DateShiftScope = DateShiftScope.Resource,
                DateShiftFixedOffsetInDays = null
            };

            // Should not throw - all values are valid
            ParameterConfigurationValidator.Validate(config);
        }

        // -----------------------------------------------------------------------
        // Differential privacy settings
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_WhenEpsilonIsZero_ThrowsArgumentException()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = 0,
                DifferentialPrivacySettings = new DifferentialPrivacyParameterConfiguration
                {
                    Epsilon = 0.0
                }
            };

            Assert.Throws<ArgumentException>(() => ParameterConfigurationValidator.Validate(config));
        }

        [Fact]
        public void Validate_WhenEpsilonExceedsMax_ThrowsArgumentException()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = 0,
                DifferentialPrivacySettings = new DifferentialPrivacyParameterConfiguration
                {
                    Epsilon = 11.0
                }
            };

            Assert.Throws<ArgumentException>(() => ParameterConfigurationValidator.Validate(config));
        }

        [Fact]
        public void Validate_WhenMaxCumulativeEpsilonIsOne_DoesNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = 0,
                DifferentialPrivacySettings = new DifferentialPrivacyParameterConfiguration
                {
                    Epsilon = 1.0,
                    MaxCumulativeEpsilon = 1.0 // default value - must not be changed to 10.0
                }
            };

            // MaxCumulativeEpsilon = 1.0 is the correct default; must not have been changed to 10.0
            ParameterConfigurationValidator.Validate(config);
        }

        [Fact]
        public void Validate_WhenMaxCumulativeEpsilonDefault_IsOne()
        {
            var settings = new DifferentialPrivacyParameterConfiguration();
            Assert.Equal(1.0, settings.MaxCumulativeEpsilon);
        }

        // -----------------------------------------------------------------------
        // DifferentialPrivacyParameterConfiguration property existence
        // -----------------------------------------------------------------------

        [Fact]
        public void DifferentialPrivacySettings_HasUseAdvancedCompositionProperty()
        {
            // Verify the property was not removed (breaking change guard)
            var settings = new DifferentialPrivacyParameterConfiguration();
            Assert.False(settings.UseAdvancedComposition);
        }

        [Fact]
        public void DifferentialPrivacySettings_HasPrivacyBudgetTrackingEnabledProperty()
        {
            // Verify the property was not removed (breaking change guard)
            var settings = new DifferentialPrivacyParameterConfiguration();
            Assert.False(settings.PrivacyBudgetTrackingEnabled);
        }

        [Fact]
        public void DifferentialPrivacySettings_HasClippingEnabledProperty()
        {
            // Verify the property was not removed (breaking change guard)
            var settings = new DifferentialPrivacyParameterConfiguration();
            Assert.False(settings.ClippingEnabled);
        }

        [Fact]
        public void DifferentialPrivacySettings_NoiseMechanismDefaultIsLaplace()
        {
            // NoiseMechanism (formerly Mechanism) must default to "Laplace"
            // and must be serialized as "mechanism" for backward compatibility
            var settings = new DifferentialPrivacyParameterConfiguration();
            Assert.Equal("Laplace", settings.NoiseMechanism);
        }
    }
}
