using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    /// <summary>
    /// Pure constant-value tests for <see cref="ParameterDefaults"/>.
    /// Behavioral Validate() tests belong in <see cref="ParameterConfigurationTests"/>.
    /// </summary>
    public class ParameterDefaultsTests
    {
        // -----------------------------------------------------------------------
        // DateShift and CryptoHash constants
        // -----------------------------------------------------------------------

        [Fact]
        public void MinDateShiftOffsetDays_HasExpectedValue()
        {
            Assert.Equal(-365, ParameterDefaults.MinDateShiftOffsetDays);
        }

        [Fact]
        public void MaxDateShiftOffsetDays_HasExpectedValue()
        {
            Assert.Equal(365, ParameterDefaults.MaxDateShiftOffsetDays);
        }

        [Fact]
        public void MinCryptoHashKeyLength_HasExpectedValue()
        {
            Assert.Equal(32, ParameterDefaults.MinCryptoHashKeyLength);
        }

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

        [Fact]
        public void DangerousPlaceholderPatterns_AllPatternsAreUppercase()
        {
            // All patterns must be stored in uppercase to match the ToUpperInvariant()
            // normalization applied to key values during validation.
            Assert.All(
                ParameterDefaults.DangerousPlaceholderPatterns,
                p => Assert.Equal(p.ToUpperInvariant(), p));
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
    }
}
