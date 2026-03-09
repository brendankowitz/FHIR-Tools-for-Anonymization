using System;
using System.Security;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    /// <summary>
    /// Unit tests for <see cref="ParameterConfiguration.Validate()"/> covering both
    /// the <see cref="ParameterConfiguration.DateShiftFixedOffsetInDays"/> range validation
    /// and the cryptographic key validation (minimum length, whitespace, placeholder and
    /// weak-pattern detection).
    /// </summary>
    public class ParameterConfigurationTests
    {
        // -----------------------------------------------------------------------
        // DateShiftFixedOffsetInDays — valid cases (should NOT throw)
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsNull_DoesNotThrow()
        {
            var config = new ParameterConfiguration { DateShiftFixedOffsetInDays = null };
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
            var config = new ParameterConfiguration { DateShiftFixedOffsetInDays = ParameterConfiguration.MinDateShiftOffsetDays };
            config.Validate();
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsAtMaxBoundary_DoesNotThrow()
        {
            var config = new ParameterConfiguration { DateShiftFixedOffsetInDays = ParameterConfiguration.MaxDateShiftOffsetDays };
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
            var config = new ParameterConfiguration { DateShiftFixedOffsetInDays = ParameterConfiguration.MinDateShiftOffsetDays - 1 };
            var ex = Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
            Assert.Contains("-366", ex.Message);
            Assert.Contains("-365", ex.Message);
            Assert.Contains("365", ex.Message);
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsAboveMax_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration { DateShiftFixedOffsetInDays = ParameterConfiguration.MaxDateShiftOffsetDays + 1 };
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
            Assert.Contains("365", ex.Message);
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsLargePositive_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration { DateShiftFixedOffsetInDays = int.MaxValue };
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
            var config = new ParameterConfiguration { DateShiftFixedOffsetInDays = offset };
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

        // -----------------------------------------------------------------------
        // CryptoHashKey tests
        // -----------------------------------------------------------------------

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void TestValidate_CryptoHashKey_NullOrEmpty_DoesNotThrow(string keyValue)
        {
            // Null / empty keys mean the feature is disabled – no exception expected
            var config = new ParameterConfiguration { CryptoHashKey = keyValue };
            Assert.Null(Record.Exception(() => config.Validate()));
        }

        [Fact]
        public void TestValidate_CryptoHashKey_WhitespaceOnly_ThrowsSecurityException()
        {
            var config = new ParameterConfiguration { CryptoHashKey = "   " };
            Assert.Throws<SecurityException>(() => config.Validate());
        }

        [Fact]
        public void TestValidate_CryptoHashKey_BelowMinimum_ThrowsSecurityException()
        {
            // One character below the 32-character minimum triggers the length check
            var config = new ParameterConfiguration { CryptoHashKey = new string('a', 31) };
            Assert.Throws<SecurityException>(() => config.Validate());
        }

        [Fact]
        public void TestValidate_CryptoHashKey_AtMinimum_DoesNotThrow()
        {
            // Exactly 32 characters – mixed characters to avoid weak-pattern checks
            var config = new ParameterConfiguration { CryptoHashKey = "aB3dEfGhIjKlMnOpQrStUvWxYz012345" };
            Assert.Null(Record.Exception(() => config.Validate()));
        }

        [Fact]
        public void TestValidate_CryptoHashKey_AboveMinimum_DoesNotThrow()
        {
            // 44-character Base64 string (typical output of: openssl rand -base64 32)
            var config = new ParameterConfiguration { CryptoHashKey = "dGhpcyBpcyBhIHZhbGlkIGJhc2U2NCBrZXkhISE=" };
            Assert.Null(Record.Exception(() => config.Validate()));
        }

        [Fact]
        public void TestValidate_CryptoHashKey_PlaceholderValue_ThrowsSecurityException()
        {
            var config = new ParameterConfiguration { CryptoHashKey = "YOUR_KEY_HERE_REPLACE_ME_PLEASE_X" };
            Assert.Throws<SecurityException>(() => config.Validate());
        }

        [Fact]
        public void TestValidate_CryptoHashKey_ShortPassword_ThrowsSecurityException()
        {
            // "password" is 8 characters – shorter than the 32-character minimum,
            // so the length check fires first (not the weak-pattern check).
            var config = new ParameterConfiguration { CryptoHashKey = "password" };
            Assert.Throws<SecurityException>(() => config.Validate());
        }

        [Fact]
        public void TestValidate_CryptoHashKey_WeakPattern_ThrowsSecurityException()
        {
            // 32 identical characters passes the length check but triggers the
            // all-same-character weak-pattern check.
            var config = new ParameterConfiguration { CryptoHashKey = new string('a', 32) };
            Assert.Throws<SecurityException>(() => config.Validate());
        }

        // -----------------------------------------------------------------------
        // EncryptKey tests (mirrors CryptoHashKey behaviour)
        // -----------------------------------------------------------------------

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void TestValidate_EncryptKey_NullOrEmpty_DoesNotThrow(string keyValue)
        {
            var config = new ParameterConfiguration { EncryptKey = keyValue };
            Assert.Null(Record.Exception(() => config.Validate()));
        }

        [Fact]
        public void TestValidate_EncryptKey_WhitespaceOnly_ThrowsSecurityException()
        {
            var config = new ParameterConfiguration { EncryptKey = "   " };
            Assert.Throws<SecurityException>(() => config.Validate());
        }

        [Fact]
        public void TestValidate_EncryptKey_BelowMinimum_ThrowsSecurityException()
        {
            var config = new ParameterConfiguration { EncryptKey = new string('x', 31) };
            Assert.Throws<SecurityException>(() => config.Validate());
        }

        [Fact]
        public void TestValidate_EncryptKey_AtMinimum_DoesNotThrow()
        {
            var config = new ParameterConfiguration { EncryptKey = "aB3dEfGhIjKlMnOpQrStUvWxYz012345" };
            Assert.Null(Record.Exception(() => config.Validate()));
        }

        [Fact]
        public void TestValidate_EncryptKey_WeakPattern_ThrowsSecurityException()
        {
            // 32 identical characters passes the length check but triggers the
            // all-same-character weak-pattern check.
            var config = new ParameterConfiguration { EncryptKey = new string('x', 32) };
            Assert.Throws<SecurityException>(() => config.Validate());
        }

        // -----------------------------------------------------------------------
        // DateShiftKey tests (mirrors CryptoHashKey behaviour)
        // -----------------------------------------------------------------------

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void TestValidate_DateShiftKey_NullOrEmpty_DoesNotThrow(string keyValue)
        {
            var config = new ParameterConfiguration { DateShiftKey = keyValue };
            Assert.Null(Record.Exception(() => config.Validate()));
        }

        [Fact]
        public void TestValidate_DateShiftKey_WhitespaceOnly_ThrowsSecurityException()
        {
            var config = new ParameterConfiguration { DateShiftKey = "   " };
            Assert.Throws<SecurityException>(() => config.Validate());
        }

        [Fact]
        public void TestValidate_DateShiftKey_BelowMinimum_ThrowsSecurityException()
        {
            var config = new ParameterConfiguration { DateShiftKey = new string('z', 31) };
            Assert.Throws<SecurityException>(() => config.Validate());
        }

        [Fact]
        public void TestValidate_DateShiftKey_AtMinimum_DoesNotThrow()
        {
            // The default auto-generated DateShiftKey is 32 chars – confirm it still passes
            var config = new ParameterConfiguration { DateShiftKey = "aB3dEfGhIjKlMnOpQrStUvWxYz012345" };
            Assert.Null(Record.Exception(() => config.Validate()));
        }

        [Fact]
        public void TestValidate_DateShiftKey_WeakPattern_ThrowsSecurityException()
        {
            // 32 identical characters passes the length check but triggers the
            // all-same-character weak-pattern check.
            var config = new ParameterConfiguration { DateShiftKey = new string('z', 32) };
            Assert.Throws<SecurityException>(() => config.Validate());
        }

        // -----------------------------------------------------------------------
        // Regression: GenerateDefaultParametersIfNotConfigured does NOT call Validate()
        // -----------------------------------------------------------------------

        [Fact]
        public void TestGenerateDefaultParameters_ShortDateShiftKey_DoesNotCallValidate()
        {
            // AnonymizerConfiguration.GenerateDefaultParametersIfNotConfigured does NOT call
            // Validate(), so providing a short key must NOT throw during generation.
            var configuration = new AnonymizerConfiguration
            {
                ParameterConfiguration = new ParameterConfiguration
                {
                    DateShiftKey = "123"
                }
            };

            // Should not throw – Validate() is not called here
            configuration.GenerateDefaultParametersIfNotConfigured();
            Assert.Equal("123", configuration.ParameterConfiguration.DateShiftKey);
        }
    }
}
