using System;
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
                DateShiftFixedOffsetInDays = null,
                DateShiftKey = "abcdefghijklmnopqrstuvwxyz123456"
            };
            config.Validate();
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsZero_DoesNotThrow()
        {
            var config = new ParameterConfiguration { DateShiftFixedOffsetInDays = 0 };
            config.Validate();
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsAtMinBoundary_DoesNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = ParameterDefaults.MinDateShiftOffsetDays
            };
            config.Validate();
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsAtMaxBoundary_DoesNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = ParameterDefaults.MaxDateShiftOffsetDays
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
            var config = new ParameterConfiguration { DateShiftFixedOffsetInDays = offset };
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
                DateShiftFixedOffsetInDays = ParameterDefaults.MinDateShiftOffsetDays - 1
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
                DateShiftFixedOffsetInDays = ParameterDefaults.MaxDateShiftOffsetDays + 1
            };
            var ex = Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
            Assert.Contains("366", ex.Message);
            Assert.Contains("-365", ex.Message);
            Assert.Contains("365", ex.Message);
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsLargeNegative_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration { DateShiftFixedOffsetInDays = int.MinValue };
            var ex = Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
            Assert.Contains(int.MinValue.ToString(), ex.Message);
            Assert.Contains("-365", ex.Message);
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsLargePositive_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration { DateShiftFixedOffsetInDays = int.MaxValue };
            var ex = Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
            Assert.Contains(int.MaxValue.ToString(), ex.Message);
            Assert.Contains("365", ex.Message);
        }

        [Theory]
        [InlineData(-366)]
        [InlineData(-1000)]
        [InlineData(366)]
        [InlineData(1000)]
        public void Validate_WhenDateShiftFixedOffsetIsOutOfRange_ThrowsAnonymizerConfigurationException(int offset)
        {
            var config = new ParameterConfiguration { DateShiftFixedOffsetInDays = offset };
            var ex = Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
            Assert.Contains(offset.ToString(), ex.Message);
            Assert.Contains("-365", ex.Message);
            Assert.Contains("365", ex.Message);
        }

        // -----------------------------------------------------------------------
        // Constants sanity checks (now referencing ParameterDefaults)
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
        // CryptoHashKey — whitespace-only (should throw SecurityException)
        // -----------------------------------------------------------------------

        [Theory]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("   ")]
        [InlineData(" \t \n ")]
        public void TestValidate_CryptoHashKey_WhitespaceOnly_ThrowsSecurityException(string key)
        {
            var config = new ParameterConfiguration { CryptoHashKey = key };
            Assert.Throws<SecurityException>(() => config.Validate());
        }

        // -----------------------------------------------------------------------
        // CryptoHashKey — below minimum length (should throw SecurityException)
        // -----------------------------------------------------------------------

        [Fact]
        public void TestValidate_CryptoHashKey_BelowMinimum_ThrowsSecurityException()
        {
            // MinCryptoHashKeyLength - 1 distinct characters: passes placeholder and weak-key
            // checks but fails the hard minimum-length requirement.
            var shortKey = new string('a', ParameterDefaults.MinCryptoHashKeyLength - 2) + "bc";
            Assert.Equal(ParameterDefaults.MinCryptoHashKeyLength - 1, shortKey.Length);

            var config = new ParameterConfiguration { CryptoHashKey = shortKey };

            var ex = Assert.Throws<SecurityException>(() => config.Validate());
            Assert.Contains((ParameterDefaults.MinCryptoHashKeyLength - 1).ToString(), ex.Message);
            Assert.Contains(ParameterDefaults.MinCryptoHashKeyLength.ToString(), ex.Message);
        }

        // -----------------------------------------------------------------------
        // CryptoHashKey — at or above minimum length (should NOT throw)
        // -----------------------------------------------------------------------

        [Fact]
        public void TestValidate_CryptoHashKey_AtMinimum_DoesNotThrow()
        {
            const string thirtyTwoCharKey = "abcdefghijklmnopqrstuvwxyz123456";
            Assert.Equal(ParameterDefaults.MinCryptoHashKeyLength, thirtyTwoCharKey.Length);

            var config = new ParameterConfiguration
            {
                CryptoHashKey = thirtyTwoCharKey,
                DateShiftFixedOffsetInDays = 0
            };
            config.Validate();
        }

        [Fact]
        public void TestValidate_CryptoHashKey_AboveMinimum_DoesNotThrow()
        {
            const string fortyCharKey = "abcdefghijklmnopqrstuvwxyz1234567890abcd";
            Assert.Equal(40, fortyCharKey.Length);

            var config = new ParameterConfiguration
            {
                CryptoHashKey = fortyCharKey,
                DateShiftFixedOffsetInDays = 0
            };
            config.Validate();
        }

        // -----------------------------------------------------------------------
        // DateShiftKey + DateShiftScope validation
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_ResourceScopeWithEmptyDateShiftKeyAndNoFixedOffset_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DateShiftScope = DateShiftScope.Resource,
                DateShiftKey = string.Empty,
                DateShiftFixedOffsetInDays = null
            };
            var ex = Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
            Assert.Contains("dateShiftKey", ex.Message);
        }

        [Fact]
        public void Validate_ResourceScopeWithNullDateShiftKeyAndNoFixedOffset_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DateShiftScope = DateShiftScope.Resource,
                DateShiftKey = null,
                DateShiftFixedOffsetInDays = null
            };
            var ex = Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
            Assert.Contains("dateShiftKey", ex.Message);
        }

        [Fact]
        public void Validate_ResourceScopeWithValidDateShiftKey_DoesNotThrow()
        {
            const string validKey = "abcdefghijklmnopqrstuvwxyz123456";
            var config = new ParameterConfiguration
            {
                DateShiftScope = DateShiftScope.Resource,
                DateShiftKey = validKey,
                DateShiftFixedOffsetInDays = null
            };
            config.Validate();
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
            var ex = Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
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
            var ex = Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
            Assert.Contains("dateShiftKey", ex.Message);
        }

        [Fact]
        public void Validate_FileScopeWithNullKeyButFixedOffsetSet_DoesNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftScope = DateShiftScope.File,
                DateShiftKey = null,
                DateShiftFixedOffsetInDays = 30
            };
            config.Validate();
        }

        // -----------------------------------------------------------------------
        // Placeholder key detection (moved from ParameterDefaultsTests)
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
        // Output markers in keys are NOT rejected (moved from ParameterDefaultsTests)
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_CryptoHashKeyContainingRedacted_DoesNotThrow()
        {
            // "REDACTED" is a legitimate output marker, not a dangerous key placeholder.
            // Construct the key to be exactly MinCryptoHashKeyLength chars so the length
            // check stays in sync automatically if the constant ever changes.
            const string prefix = "REDACTED_";
            var key = prefix + new string('a', ParameterDefaults.MinCryptoHashKeyLength - prefix.Length);
            Assert.Equal(ParameterDefaults.MinCryptoHashKeyLength, key.Length);

            var config = new ParameterConfiguration
            {
                CryptoHashKey = key,
                DateShiftFixedOffsetInDays = 0
            };
            config.Validate();
        }

        // -----------------------------------------------------------------------
        // Differential privacy epsilon bounds (moved from ParameterDefaultsTests)
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
        [InlineData(1.0)]   // Default — must NOT warn after fixing >= to > in the condition
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
        // K-anonymity k-value bounds (moved from ParameterDefaultsTests)
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
