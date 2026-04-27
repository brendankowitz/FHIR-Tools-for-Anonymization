using System.Security;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
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
                DateShiftFixedOffsetInDays = null,
                // DateShiftKey required when DateShiftFixedOffsetInDays is null and scope is Resource
                DateShiftKey = "abcdefghijklmnopqrstuvwxyz123456"
            };

            // Should not throw - null means "use key-based shift"; key is provided
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
                DateShiftFixedOffsetInDays = ParameterDefaults.MinDateShiftOffsetDays // -365
            };

            config.Validate();
        }

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsAtMaxBoundary_DoesNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = ParameterDefaults.MaxDateShiftOffsetDays // +365
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
        // DateShiftFixedOffsetInDays - invalid cases (should throw)
        // -----------------------------------------------------------------------

        [Fact]
        public void Validate_WhenDateShiftFixedOffsetIsBelowMin_ThrowsAnonymizerConfigurationException()
        {
            var config = new ParameterConfiguration
            {
                DateShiftFixedOffsetInDays = ParameterDefaults.MinDateShiftOffsetDays - 1 // -366
            };

            var ex = Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
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

            var ex = Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
            Assert.Contains("366", ex.Message);
            Assert.Contains(ParameterDefaults.MinDateShiftOffsetDays.ToString(), ex.Message);
            Assert.Contains(ParameterDefaults.MaxDateShiftOffsetDays.ToString(), ex.Message);
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
            // Both bounds must appear in the error message so the caller knows the valid range.
            Assert.Contains(ParameterDefaults.MinDateShiftOffsetDays.ToString(), ex.Message);
            Assert.Contains(ParameterDefaults.MaxDateShiftOffsetDays.ToString(), ex.Message);
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
            // Both bounds must appear in the error message so the caller knows the valid range.
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

            var ex = Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
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

            Assert.Throws<SecurityException>(() => config.Validate());
        }

        // -----------------------------------------------------------------------
        // CryptoHashKey - below minimum length (should throw SecurityException)
        // -----------------------------------------------------------------------

        [Fact]
        public void TestValidate_CryptoHashKey_BelowMinimum_ThrowsSecurityException()
        {
            // Construct a key that is exactly MinCryptoHashKeyLength - 1 characters long.
            // Use distinct trailing characters to avoid triggering the all-same-char weak-key
            // check, which fires before the length check.
            //
            // Formula: (MinCryptoHashKeyLength - 3) 'a' chars + "bc"
            //        = (32 - 3) + 2 = 31 chars = MinCryptoHashKeyLength - 1
            var shortKey = new string('a', ParameterDefaults.MinCryptoHashKeyLength - 3) + "bc";
            Assert.Equal(ParameterDefaults.MinCryptoHashKeyLength - 1, shortKey.Length);

            var config = new ParameterConfiguration
            {
                CryptoHashKey = shortKey
            };

            var ex = Assert.Throws<SecurityException>(() => config.Validate());
            // Error message must report the actual length and the required minimum.
            Assert.Contains((ParameterDefaults.MinCryptoHashKeyLength - 1).ToString(), ex.Message);
            Assert.Contains(ParameterDefaults.MinCryptoHashKeyLength.ToString(), ex.Message);
        }

        // -----------------------------------------------------------------------
        // CryptoHashKey - at minimum length (should NOT throw)
        // -----------------------------------------------------------------------

        [Fact]
        public void TestValidate_CryptoHashKey_AtMinimum_DoesNotThrow()
        {
            // Exactly 32 characters composed of distinct characters to avoid weak-key detection.
            const string thirtyTwoCharKey = "abcdefghijklmnopqrstuvwxyz123456"; // 32 chars
            Assert.Equal(ParameterDefaults.MinCryptoHashKeyLength, thirtyTwoCharKey.Length);

            var config = new ParameterConfiguration
            {
                CryptoHashKey = thirtyTwoCharKey,
                // Provide a fixed offset so no DateShiftKey is required for Resource scope
                DateShiftFixedOffsetInDays = 0
            };

            // Should not throw - exactly meets the minimum length requirement.
            config.Validate();
        }

        // -----------------------------------------------------------------------
        // CryptoHashKey - above minimum length (should NOT throw)
        // -----------------------------------------------------------------------

        [Fact]
        public void TestValidate_CryptoHashKey_AboveMinimum_DoesNotThrow()
        {
            // 40 characters - comfortably above the 32-character minimum.
            const string fortyCharKey = "abcdefghijklmnopqrstuvwxyz1234567890abcd"; // 40 chars
            Assert.Equal(40, fortyCharKey.Length);

            var config = new ParameterConfiguration
            {
                CryptoHashKey = fortyCharKey,
                // Provide a fixed offset so no DateShiftKey is required for Resource scope
                DateShiftFixedOffsetInDays = 0
            };

            // Should not throw - exceeds the minimum length requirement.
            config.Validate();
        }

        // -----------------------------------------------------------------------
        // DateShiftKey + DateShiftScope validation
        // -----------------------------------------------------------------------

        /// <summary>
        /// Resource scope requires a dateShiftKey when dateShiftFixedOffsetInDays is not set.
        /// The implementation enforces this for ALL scopes (Resource, File, Folder) to prevent
        /// re-identification attacks. An empty key with no fixed offset must throw.
        /// </summary>
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

        /// <summary>
        /// A null key with no fixed offset must throw for all scopes including Resource.
        /// </summary>
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

        /// <summary>
        /// Resource scope with a valid (non-empty) dateShiftKey and no fixed offset must NOT throw.
        /// </summary>
        [Fact]
        public void Validate_ResourceScopeWithValidDateShiftKey_DoesNotThrow()
        {
            // 32-character key - meets length requirements; no fixed offset forces key-based shifting.
            const string validKey = "abcdefghijklmnopqrstuvwxyz123456"; // 32 chars
            var config = new ParameterConfiguration
            {
                DateShiftScope = DateShiftScope.Resource,
                DateShiftKey = validKey,
                DateShiftFixedOffsetInDays = null
            };

            // Should NOT throw - a valid key is present; key-based date shifting is fully configured.
            config.Validate();
        }

        /// <summary>
        /// File scope requires a deterministic key so that all resources in the same file
        /// receive consistent date shifts. Missing key with no fixed offset must throw.
        /// </summary>
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

        /// <summary>
        /// Folder scope requires a deterministic key so that all resources in the same folder
        /// receive consistent date shifts. Missing key with no fixed offset must throw.
        /// </summary>
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
            // When DateShiftFixedOffsetInDays is set, no DateShiftKey is required even for File scope
            var config = new ParameterConfiguration
            {
                DateShiftScope = DateShiftScope.File,
                DateShiftKey = null,
                DateShiftFixedOffsetInDays = 30
            };

            // Should not throw - fixed offset is provided, key is not needed
            config.Validate();
        }
    }
}
