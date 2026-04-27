// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    /// <summary>
    /// Tests that assert the exact expected values of every entry in
    /// <see cref="ParameterDefaults"/>. Any unintentional drift in a default value
    /// will cause one of these tests to fail, making compliance regressions
    /// immediately visible in CI.
    /// </summary>
    public class ParameterDefaultsTests
    {
        // -------------------------------------------------------------------
        // DateShift
        // -------------------------------------------------------------------

        [Fact]
        public void MinDateShiftOffsetDays_ShouldBeMinus365()
        {
            Assert.Equal(-365, ParameterDefaults.MinDateShiftOffsetDays);
        }

        [Fact]
        public void MaxDateShiftOffsetDays_ShouldBe365()
        {
            Assert.Equal(365, ParameterDefaults.MaxDateShiftOffsetDays);
        }

        // -------------------------------------------------------------------
        // CryptoHash
        // -------------------------------------------------------------------

        [Fact]
        public void MinCryptoHashKeyLength_ShouldBe32()
        {
            Assert.Equal(32, ParameterDefaults.MinCryptoHashKeyLength);
        }

        // -------------------------------------------------------------------
        // Encrypt – AES key sizes
        // -------------------------------------------------------------------

        [Fact]
        public void ValidAesKeySizeBits_ShouldContainExactlyThreeSizes()
        {
            Assert.Equal(3, ParameterDefaults.ValidAesKeySizeBits.Count);
        }

        [Theory]
        [InlineData(128)]
        [InlineData(192)]
        [InlineData(256)]
        public void ValidAesKeySizeBits_ShouldContain(int expectedSize)
        {
            Assert.Contains(expectedSize, ParameterDefaults.ValidAesKeySizeBits);
        }

        [Theory]
        [InlineData(64)]
        [InlineData(512)]
        [InlineData(0)]
        public void ValidAesKeySizeBits_ShouldNotContainInvalidSize(int invalidSize)
        {
            Assert.DoesNotContain(invalidSize, ParameterDefaults.ValidAesKeySizeBits);
        }

        // -------------------------------------------------------------------
        // Security – dangerous placeholder patterns
        // -------------------------------------------------------------------

        [Fact]
        public void DangerousPlaceholderPatterns_ShouldBeNonEmpty()
        {
            Assert.NotEmpty(ParameterDefaults.DangerousPlaceholderPatterns);
        }

        [Theory]
        [InlineData("REDACTED")]
        [InlineData("[REDACTED]")]
        [InlineData("***")]
        public void DangerousPlaceholderPatterns_ShouldContainKnownSentinels(string sentinel)
        {
            Assert.Contains(sentinel, ParameterDefaults.DangerousPlaceholderPatterns);
        }

        // -------------------------------------------------------------------
        // Redact
        // -------------------------------------------------------------------

        [Fact]
        public void EnablePartialAgesForRedact_DefaultShouldBeFalse()
        {
            Assert.False(ParameterDefaults.EnablePartialAgesForRedact);
        }

        [Fact]
        public void EnablePartialDatesForRedact_DefaultShouldBeFalse()
        {
            Assert.False(ParameterDefaults.EnablePartialDatesForRedact);
        }

        [Fact]
        public void EnablePartialZipCodesForRedact_DefaultShouldBeFalse()
        {
            Assert.False(ParameterDefaults.EnablePartialZipCodesForRedact);
        }

        // -------------------------------------------------------------------
        // Redact – ParameterConfiguration property defaults must match
        // -------------------------------------------------------------------

        [Fact]
        public void ParameterConfiguration_EnablePartialAgesForRedact_ShouldMatchDefault()
        {
            var config = new ParameterConfiguration();
            Assert.Equal(ParameterDefaults.EnablePartialAgesForRedact, config.EnablePartialAgesForRedact);
        }

        [Fact]
        public void ParameterConfiguration_EnablePartialDatesForRedact_ShouldMatchDefault()
        {
            var config = new ParameterConfiguration();
            Assert.Equal(ParameterDefaults.EnablePartialDatesForRedact, config.EnablePartialDatesForRedact);
        }

        [Fact]
        public void ParameterConfiguration_EnablePartialZipCodesForRedact_ShouldMatchDefault()
        {
            var config = new ParameterConfiguration();
            Assert.Equal(ParameterDefaults.EnablePartialZipCodesForRedact, config.EnablePartialZipCodesForRedact);
        }

        // -------------------------------------------------------------------
        // Backward-compatible API constants on ParameterConfiguration
        // -------------------------------------------------------------------

        [Fact]
        public void ParameterConfiguration_MinDateShiftOffsetDays_ShouldForwardToDefaults()
        {
            Assert.Equal(ParameterDefaults.MinDateShiftOffsetDays,
                ParameterConfiguration.MinDateShiftOffsetDays);
        }

        [Fact]
        public void ParameterConfiguration_MaxDateShiftOffsetDays_ShouldForwardToDefaults()
        {
            Assert.Equal(ParameterDefaults.MaxDateShiftOffsetDays,
                ParameterConfiguration.MaxDateShiftOffsetDays);
        }

        [Fact]
        public void ParameterConfiguration_MinCryptoHashKeyLength_ShouldForwardToDefaults()
        {
            Assert.Equal(ParameterDefaults.MinCryptoHashKeyLength,
                ParameterConfiguration.MinCryptoHashKeyLength);
        }

        // -------------------------------------------------------------------
        // KAnonymity
        // -------------------------------------------------------------------

        [Fact]
        public void KValue_DefaultShouldBe5()
        {
            Assert.Equal(5, ParameterDefaults.KValue);
        }

        [Fact]
        public void SuppressionThreshold_DefaultShouldBe0Point3()
        {
            Assert.Equal(0.3, ParameterDefaults.SuppressionThreshold);
        }

        [Fact]
        public void KAnonymityParameterConfiguration_KValue_ShouldMatchDefault()
        {
            var config = new KAnonymityParameterConfiguration();
            Assert.Equal(ParameterDefaults.KValue, config.KValue);
        }

        [Fact]
        public void KAnonymityParameterConfiguration_SuppressionThreshold_ShouldMatchDefault()
        {
            var config = new KAnonymityParameterConfiguration();
            Assert.Equal(ParameterDefaults.SuppressionThreshold, config.SuppressionThreshold);
        }

        // -------------------------------------------------------------------
        // DifferentialPrivacy
        // -------------------------------------------------------------------

        [Fact]
        public void Epsilon_DefaultShouldBe1Point0()
        {
            Assert.Equal(1.0, ParameterDefaults.Epsilon);
        }

        [Fact]
        public void Delta_DefaultShouldBe1e5()
        {
            Assert.Equal(1e-5, ParameterDefaults.Delta);
        }

        [Fact]
        public void Sensitivity_DefaultShouldBe1Point0()
        {
            Assert.Equal(1.0, ParameterDefaults.Sensitivity);
        }

        [Fact]
        public void MaxCumulativeEpsilon_DefaultShouldBe1Point0()
        {
            Assert.Equal(1.0, ParameterDefaults.MaxCumulativeEpsilon);
        }

        [Fact]
        public void UseAdvancedComposition_DefaultShouldBeFalse()
        {
            Assert.False(ParameterDefaults.UseAdvancedComposition);
        }

        [Fact]
        public void Mechanism_DefaultShouldBeLaplace()
        {
            Assert.Equal("laplace", ParameterDefaults.Mechanism);
        }

        [Fact]
        public void PrivacyBudgetTrackingEnabled_DefaultShouldBeFalse()
        {
            Assert.False(ParameterDefaults.PrivacyBudgetTrackingEnabled);
        }

        [Fact]
        public void ClippingEnabled_DefaultShouldBeFalse()
        {
            Assert.False(ParameterDefaults.ClippingEnabled);
        }

        [Fact]
        public void DifferentialPrivacyParameterConfiguration_Epsilon_ShouldMatchDefault()
        {
            var config = new DifferentialPrivacyParameterConfiguration();
            Assert.Equal(ParameterDefaults.Epsilon, config.Epsilon);
        }

        [Fact]
        public void DifferentialPrivacyParameterConfiguration_Delta_ShouldMatchDefault()
        {
            var config = new DifferentialPrivacyParameterConfiguration();
            Assert.Equal(ParameterDefaults.Delta, config.Delta);
        }

        [Fact]
        public void DifferentialPrivacyParameterConfiguration_Sensitivity_ShouldMatchDefault()
        {
            var config = new DifferentialPrivacyParameterConfiguration();
            Assert.Equal(ParameterDefaults.Sensitivity, config.Sensitivity);
        }

        [Fact]
        public void DifferentialPrivacyParameterConfiguration_MaxCumulativeEpsilon_ShouldMatchDefault()
        {
            var config = new DifferentialPrivacyParameterConfiguration();
            Assert.Equal(ParameterDefaults.MaxCumulativeEpsilon, config.MaxCumulativeEpsilon);
        }

        [Fact]
        public void DifferentialPrivacyParameterConfiguration_UseAdvancedComposition_ShouldMatchDefault()
        {
            var config = new DifferentialPrivacyParameterConfiguration();
            Assert.Equal(ParameterDefaults.UseAdvancedComposition, config.UseAdvancedComposition);
        }

        [Fact]
        public void DifferentialPrivacyParameterConfiguration_Mechanism_ShouldMatchDefault()
        {
            var config = new DifferentialPrivacyParameterConfiguration();
            Assert.Equal(ParameterDefaults.Mechanism, config.Mechanism);
        }

        [Fact]
        public void DifferentialPrivacyParameterConfiguration_PrivacyBudgetTrackingEnabled_ShouldMatchDefault()
        {
            var config = new DifferentialPrivacyParameterConfiguration();
            Assert.Equal(ParameterDefaults.PrivacyBudgetTrackingEnabled, config.PrivacyBudgetTrackingEnabled);
        }

        [Fact]
        public void DifferentialPrivacyParameterConfiguration_ClippingEnabled_ShouldMatchDefault()
        {
            var config = new DifferentialPrivacyParameterConfiguration();
            Assert.Equal(ParameterDefaults.ClippingEnabled, config.ClippingEnabled);
        }
    }
}
