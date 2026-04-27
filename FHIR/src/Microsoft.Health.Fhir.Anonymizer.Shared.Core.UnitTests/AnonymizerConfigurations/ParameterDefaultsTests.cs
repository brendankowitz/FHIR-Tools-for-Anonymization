using System;
using System.Security;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    public class ParameterDefaultsTests
    {
        // -----------------------------------------------------------------------
        // DangerousPlaceholderPatterns
        // -----------------------------------------------------------------------

        [Fact]
        public void DangerousPlaceholderPatterns_ShouldContainKnownSentinels()
        {
            var patterns = ParameterDefaults.DangerousPlaceholderPatterns;

            Assert.Contains("YOUR_KEY_HERE", patterns);
            Assert.Contains("PLACEHOLDER", patterns);
            Assert.Contains("CHANGE_ME", patterns);
            Assert.Contains("TEST_KEY", patterns);
            Assert.Contains("TODO", patterns);
            Assert.Contains("FIXME", patterns);
            Assert.Contains("$HMAC_KEY", patterns);
            Assert.Contains("CHANGEME", patterns);
            Assert.Contains("REPLACE_ME", patterns);
        }

        [Fact]
        public void DangerousPlaceholderPatterns_ShouldNotContainAnonymizationOutputMarkers()
        {
            var patterns = ParameterDefaults.DangerousPlaceholderPatterns;

            Assert.DoesNotContain("REDACTED", patterns);
            Assert.DoesNotContain("[REDACTED]", patterns);
            Assert.DoesNotContain("***", patterns);
            Assert.DoesNotContain("ANONYMIZED", patterns);
        }

        [Fact]
        public void DangerousPlaceholderPatterns_ShouldNotBeEmpty()
        {
            Assert.NotEmpty(ParameterDefaults.DangerousPlaceholderPatterns);
        }

        // -----------------------------------------------------------------------
        // AnonymizationOutputMarkers
        // -----------------------------------------------------------------------

        [Fact]
        public void AnonymizationOutputMarkers_ShouldContainKnownMarkers()
        {
            var markers = ParameterDefaults.AnonymizationOutputMarkers;

            Assert.Contains("REDACTED", markers);
            Assert.Contains("[REDACTED]", markers);
            Assert.Contains("***", markers);
            Assert.Contains("ANONYMIZED", markers);
        }

        [Fact]
        public void AnonymizationOutputMarkers_ShouldNotBeEmpty()
        {
            Assert.NotEmpty(ParameterDefaults.AnonymizationOutputMarkers);
        }

        [Fact]
        public void AnonymizationOutputMarkers_ShouldNotOverlapWithDangerousPlaceholderPatterns()
        {
            foreach (var marker in ParameterDefaults.AnonymizationOutputMarkers)
            {
                Assert.DoesNotContain(marker, ParameterDefaults.DangerousPlaceholderPatterns);
            }
        }

        // -----------------------------------------------------------------------
        // ValidAesKeySizeBits
        // -----------------------------------------------------------------------

        [Fact]
        public void ValidAesKeySizeBits_ShouldContainExpectedSizes()
        {
            var sizes = ParameterDefaults.ValidAesKeySizeBits;

            Assert.Contains(128, sizes);
            Assert.Contains(192, sizes);
            Assert.Contains(256, sizes);
            Assert.Equal(3, sizes.Count);
        }

        [Theory]
        [InlineData(64)]
        [InlineData(96)]
        [InlineData(160)]
        [InlineData(512)]
        public void ValidAesKeySizeBits_ShouldNotContainInvalidSizes(int invalidSize)
        {
            Assert.DoesNotContain(invalidSize, ParameterDefaults.ValidAesKeySizeBits);
        }

        // -----------------------------------------------------------------------
        // Validate() - DateShiftFixedOffsetInDays range
        // -----------------------------------------------------------------------

        [Theory]
        [InlineData(-366)]
        [InlineData(-1000)]
        [InlineData(366)]
        [InlineData(1000)]
        public void Validate_DateShiftFixedOffsetOutOfRange_ThrowsAnonymizerConfigurationException(int offset)
        {
            var config = new ParameterConfiguration { DateShiftFixedOffsetInDays = offset };

            var ex = Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
            Assert.Contains(offset.ToString(), ex.Message);
            Assert.Contains("-365", ex.Message);
            Assert.Contains("365", ex.Message);
        }

        [Theory]
        [InlineData(-365)]
        [InlineData(0)]
        [InlineData(365)]
        public void Validate_DateShiftFixedOffsetInRange_DoesNotThrow(int offset)
        {
            var config = new ParameterConfiguration { DateShiftFixedOffsetInDays = offset };
            config.Validate();
        }

        // -----------------------------------------------------------------------
        // Validate() - placeholder key detection
        // -----------------------------------------------------------------------

        [Theory]
        [InlineData("YOUR_KEY_HERE")]
        [InlineData("placeholder_value")]
        [InlineData("change_me")]
        [InlineData("test_key_value")]
        public void Validate_CryptoHashKeyWithPlaceholder_ThrowsSecurityException(string key)
        {
            var config = new ParameterConfiguration
            {
                CryptoHashKey = key,
                DateShiftFixedOffsetInDays = 0
            };
            Assert.Throws<SecurityException>(() => config.Validate());
        }

        [Theory]
        [InlineData("YOUR_KEY_HERE")]
        [InlineData("placeholder_value")]
        [InlineData("change_me")]
        public void Validate_DateShiftKeyWithPlaceholder_ThrowsSecurityException(string key)
        {
            var config = new ParameterConfiguration
            {
                DateShiftKey = key,
                DateShiftFixedOffsetInDays = 0
            };
            Assert.Throws<SecurityException>(() => config.Validate());
        }

        [Theory]
        [InlineData("YOUR_KEY_HERE")]
        [InlineData("placeholder_value")]
        [InlineData("change_me")]
        public void Validate_EncryptKeyWithPlaceholder_ThrowsSecurityException(string key)
        {
            var config = new ParameterConfiguration
            {
                EncryptKey = key,
                DateShiftFixedOffsetInDays = 0
            };
            Assert.Throws<SecurityException>(() => config.Validate());
        }

        // -----------------------------------------------------------------------
        // Validate() - output markers in keys are NOT rejected
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_CryptoHashKeyContainingRedacted_DoesNotThrowSecurityException()
        {
            // "REDACTED" is a legitimate output marker, not a dangerous key placeholder.
            const string key = "REDACTED_abcdefghijklmnopqrstuvw"; // 32 chars
            Assert.Equal(32, key.Length);

            var config = new ParameterConfiguration
            {
                CryptoHashKey = key,
                DateShiftFixedOffsetInDays = 0
            };
            config.Validate();
        }

        // -----------------------------------------------------------------------
        // Validate() - differential privacy epsilon bounds
        // -----------------------------------------------------------------------

        [Theory]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        [InlineData(-0.001)]
        public void Validate_EpsilonZeroOrNegative_ThrowsArgumentException(double epsilon)
        {
            var config = new ParameterConfiguration
            {
                DifferentialPrivacySettings = new DifferentialPrivacyParameterConfiguration
                {
                    Epsilon = epsilon, Sensitivity = 1.0, MaxCumulativeEpsilon = 1.0
                },
                DateShiftFixedOffsetInDays = 0
            };
            Assert.Throws<ArgumentException>(() => config.Validate());
        }

        [Theory]
        [InlineData(10.1)]
        [InlineData(100.0)]
        public void Validate_EpsilonAboveMaximum_ThrowsArgumentException(double epsilon)
        {
            var config = new ParameterConfiguration
            {
                DifferentialPrivacySettings = new DifferentialPrivacyParameterConfiguration
                {
                    Epsilon = epsilon, Sensitivity = 1.0, MaxCumulativeEpsilon = 1.0
                },
                DateShiftFixedOffsetInDays = 0
            };
            Assert.Throws<ArgumentException>(() => config.Validate());
        }

        [Theory]
        [InlineData(0.001)]
        [InlineData(0.1)]
        [InlineData(0.99)]
        [InlineData(9.99)]
        [InlineData(10.0)]
        public void Validate_EpsilonWithinValidRange_DoesNotThrow(double epsilon)
        {
            var config = new ParameterConfiguration
            {
                DifferentialPrivacySettings = new DifferentialPrivacyParameterConfiguration
                {
                    Epsilon = epsilon, Sensitivity = 1.0, MaxCumulativeEpsilon = 1.0
                },
                DateShiftFixedOffsetInDays = 0
            };
            config.Validate();
        }

        // -----------------------------------------------------------------------
        // Validate() - k-anonymity k-value bounds
        // -----------------------------------------------------------------------

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-5)]
        public void Validate_KValueLessThanTwo_ThrowsArgumentException(int kValue)
        {
            var config = new ParameterConfiguration
            {
                KAnonymitySettings = new KAnonymityParameterConfiguration { KValue = kValue },
                DateShiftFixedOffsetInDays = 0
            };
            var ex = Assert.Throws<ArgumentException>(() => config.Validate());
            Assert.Contains(kValue.ToString(), ex.Message);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(100)]
        public void Validate_KValueTwoOrGreater_DoesNotThrow(int kValue)
        {
            var config = new ParameterConfiguration
            {
                KAnonymitySettings = new KAnonymityParameterConfiguration { KValue = kValue },
                DateShiftFixedOffsetInDays = 0
            };
            config.Validate();
        }
    }
}
