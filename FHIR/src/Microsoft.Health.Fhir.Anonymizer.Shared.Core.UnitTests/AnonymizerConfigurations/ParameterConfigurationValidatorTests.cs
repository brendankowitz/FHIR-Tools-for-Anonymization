using System;
using System.Linq;
using System.Security;
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
        // DateShiftKey minimum length enforcement
        // -----------------------------------------------------------------------

        /// <summary>
        /// A DateShiftKey that is non-null/non-empty but shorter than
        /// <see cref="ParameterDefaults.MinDateShiftKeyLength"/> must be rejected with a
        /// <see cref="SecurityException"/> regardless of the configured scope.
        /// </summary>
        /// <remarks>
        /// InlineData starts at 2 (not 1) because BuildValidKeyOfLength(1) returns a
        /// single-character string (e.g. "a") which triggers the all-same-character
        /// weak-key guard in ValidateKeyParameter before the minimum-length check in
        /// ValidateDateShiftKeyForScope is reached. Length 2 ("ab") passes the
        /// all-same-character check and correctly reaches the length guard.
        /// </remarks>
        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(ParameterDefaults.MinDateShiftKeyLength - 1)]
        public void Validate_WhenDateShiftKeyIsShorterThanMinimum_ThrowsSecurityException(int keyLength)
        {
            // Build a key that is the right length but otherwise valid
            // (non-placeholder, non-weak, mixed characters)
            var shortKey = BuildValidKeyOfLength(keyLength);

            var config = new ParameterConfiguration
            {
                DateShiftKey = shortKey,
                DateShiftScope = DateShiftScope.Resource
            };

            var ex = Assert.Throws<SecurityException>(() =>
                ParameterConfigurationValidator.Validate(config));

            Assert.Contains(ParameterDefaults.MinDateShiftKeyLength.ToString(), ex.Message);
        }

        // -----------------------------------------------------------------------
        // CryptoHashKey minimum length enforcement
        // -----------------------------------------------------------------------

        /// <summary>
        /// A CryptoHashKey shorter than <see cref="ParameterDefaults.MinCryptoHashKeyLength"/>
        /// must be rejected with a <see cref="SecurityException"/>.
        /// Parameterised over several sub-minimum lengths to avoid relying on a single
        /// magic string literal whose length might silently change.
        /// </summary>
        /// <remarks>
        /// InlineData starts at 2 (not 1) because BuildValidKeyOfLength(1) returns a
        /// single-character string (e.g. "a") which triggers the all-same-character
        /// weak-key guard in ValidateKeyParameter before the minimum-length check is
        /// reached. Length 2 ("ab") passes the all-same-character check and correctly
        /// reaches the length guard, producing a SecurityException whose message
        /// contains the required minimum length.
        /// </remarks>
        [Theory]
        [InlineData(2)]
        [InlineData(10)]
        [InlineData(ParameterDefaults.MinCryptoHashKeyLength - 1)]
        public void Validate_WhenCryptoHashKeyIsShorterThanMinimum_ThrowsSecurityException(int keyLength)
        {
            var shortKey = BuildValidKeyOfLength(keyLength);

            var config = new ParameterConfiguration
            {
                CryptoHashKey = shortKey,
                // Provide a fixed offset to satisfy the DateShiftKey requirement
                DateShiftFixedOffsetInDays = 0
            };

            var ex = Assert.Throws<SecurityException>(() =>
                ParameterConfigurationValidator.Validate(config));

            Assert.Contains(ParameterDefaults.MinCryptoHashKeyLength.ToString(), ex.Message);
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

        /// <summary>
        /// Verifies that the <c>Mechanism</c> property (deprecated shim for <c>NoiseMechanism</c>)
        /// still forwards reads and writes correctly so that existing callers are not broken
        /// during the deprecation window.
        /// </summary>
        [Fact]
        public void DifferentialPrivacySettings_MechanismShimForwardsToNoiseMechanism()
        {
            var settings = new DifferentialPrivacyParameterConfiguration();

            // Read-through: Mechanism should return whatever NoiseMechanism holds
            Assert.Equal(settings.NoiseMechanism, settings.Mechanism);

            // Write-through: setting Mechanism should update NoiseMechanism
#pragma warning disable CS0618 // intentionally testing the obsolete shim
            settings.Mechanism = "Gaussian";
#pragma warning restore CS0618
            Assert.Equal("Gaussian", settings.NoiseMechanism);
        }

        // -----------------------------------------------------------------------
        // Helper methods
        // -----------------------------------------------------------------------

        /// <summary>
        /// Builds a key of the given <paramref name="length"/> that passes all
        /// placeholder and weak-key checks so that only a length-related guard fires.
        /// Uses a repeating alphabet sequence ("abcdefghij...") to ensure no single
        /// repeated character triggers the "all same character" weak-key rejection.
        /// Note: length must be >= 2 to guarantee at least two distinct characters
        /// and avoid the all-same-character weak-key check.
        /// </summary>
        private static string BuildValidKeyOfLength(int length)
        {
            return string.Concat(Enumerable.Range(0, length).Select(i => (char)('a' + i % 26)));
        }
    }
}
