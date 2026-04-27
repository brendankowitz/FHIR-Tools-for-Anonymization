using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    public class ParameterDefaultsTests
    {
        // -----------------------------------------------------------------------
        // Constant value sanity checks
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

        [Fact]
        public void MinDateShiftOffsetDays_IsNegative()
        {
            Assert.True(
                ParameterDefaults.MinDateShiftOffsetDays < 0,
                "MinDateShiftOffsetDays should be negative.");
        }

        [Fact]
        public void MaxDateShiftOffsetDays_IsPositive()
        {
            Assert.True(
                ParameterDefaults.MaxDateShiftOffsetDays > 0,
                "MaxDateShiftOffsetDays should be positive.");
        }

        [Fact]
        public void MinDateShiftOffsetDays_IsLessThanMaxDateShiftOffsetDays()
        {
            Assert.True(
                ParameterDefaults.MinDateShiftOffsetDays < ParameterDefaults.MaxDateShiftOffsetDays,
                "MinDateShiftOffsetDays must be less than MaxDateShiftOffsetDays.");
        }

        [Fact]
        public void MinCryptoHashKeyLength_IsPositive()
        {
            Assert.True(
                ParameterDefaults.MinCryptoHashKeyLength > 0,
                "MinCryptoHashKeyLength should be positive.");
        }

        // -----------------------------------------------------------------------
        // DangerousPlaceholderPatterns
        // -----------------------------------------------------------------------

        [Fact]
        public void DangerousPlaceholderPatterns_IsNotEmpty()
        {
            Assert.NotEmpty(ParameterDefaults.DangerousPlaceholderPatterns);
        }

        [Fact]
        public void DangerousPlaceholderPatterns_AllEntriesAreNonEmptyStrings()
        {
            // Every entry must be a meaningful non-whitespace string.
            // A null, empty, or whitespace-only pattern would silently match any key and
            // break the security validation contract.
            foreach (var pattern in ParameterDefaults.DangerousPlaceholderPatterns)
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(pattern),
                    "DangerousPlaceholderPatterns contains a null, empty, or whitespace-only entry.");
            }
        }

        [Fact]
        public void DangerousPlaceholderPatterns_ContainsCommonPlaceholders()
        {
            Assert.Contains("YOUR_KEY_HERE", ParameterDefaults.DangerousPlaceholderPatterns);
            Assert.Contains("PLACEHOLDER", ParameterDefaults.DangerousPlaceholderPatterns);
            Assert.Contains("CHANGE_ME", ParameterDefaults.DangerousPlaceholderPatterns);
        }

        // -----------------------------------------------------------------------
        // AnonymizationOutputMarkers
        // -----------------------------------------------------------------------

        [Fact]
        public void AnonymizationOutputMarkers_IsNotEmpty()
        {
            Assert.NotEmpty(ParameterDefaults.AnonymizationOutputMarkers);
        }

        [Fact]
        public void AnonymizationOutputMarkers_AllEntriesAreNonEmptyStrings()
        {
            foreach (var marker in ParameterDefaults.AnonymizationOutputMarkers)
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(marker),
                    "AnonymizationOutputMarkers contains a null, empty, or whitespace-only entry.");
            }
        }

        [Fact]
        public void AnonymizationOutputMarkers_ContainsRedacted()
        {
            Assert.Contains("REDACTED", ParameterDefaults.AnonymizationOutputMarkers);
        }

        // -----------------------------------------------------------------------
        // Backward-compatibility: [Obsolete] forwarding constants on ParameterConfiguration
        // -----------------------------------------------------------------------

        [Fact]
        public void ParameterConfiguration_ObsoleteConstants_ForwardToParameterDefaults()
        {
            // Suppress the [Obsolete] warnings; this test explicitly verifies the forwarding.
#pragma warning disable CS0618
            Assert.Equal(ParameterDefaults.MinDateShiftOffsetDays, ParameterConfiguration.MinDateShiftOffsetDays);
            Assert.Equal(ParameterDefaults.MaxDateShiftOffsetDays, ParameterConfiguration.MaxDateShiftOffsetDays);
            Assert.Equal(ParameterDefaults.MinCryptoHashKeyLength, ParameterConfiguration.MinCryptoHashKeyLength);
#pragma warning restore CS0618
        }
    }
}
