// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    public class ParameterConfiguration
    {
        // ---------------------------------------------------------------------------
        // Public constants kept for backward API compatibility.
        // Their values are forwarded from ParameterDefaults so there is a single
        // source of truth for every default or boundary constant in this codebase.
        // ---------------------------------------------------------------------------

        /// <inheritdoc cref="ParameterDefaults.MinDateShiftOffsetDays"/>
        public const int MinDateShiftOffsetDays = ParameterDefaults.MinDateShiftOffsetDays;

        /// <inheritdoc cref="ParameterDefaults.MaxDateShiftOffsetDays"/>
        public const int MaxDateShiftOffsetDays = ParameterDefaults.MaxDateShiftOffsetDays;

        /// <inheritdoc cref="ParameterDefaults.MinCryptoHashKeyLength"/>
        public const int MinCryptoHashKeyLength = ParameterDefaults.MinCryptoHashKeyLength;

        // ---------------------------------------------------------------------------
        // Private validation helpers sourced from ParameterDefaults.
        // ---------------------------------------------------------------------------

        private static readonly IReadOnlySet<int> s_validAesKeySizeBits =
            ParameterDefaults.ValidAesKeySizeBits;

        private static readonly IReadOnlyList<string> s_dangerousPlaceholderPatterns =
            ParameterDefaults.DangerousPlaceholderPatterns;

        // ---------------------------------------------------------------------------
        // Top-level settings
        // ---------------------------------------------------------------------------

        [JsonProperty("dateShift")]
        public DateShiftConfiguration DateShift { get; set; }

        [JsonProperty("cryptoHash")]
        public CryptoHashConfiguration CryptoHash { get; set; }

        [JsonProperty("encrypt")]
        public EncryptConfiguration Encrypt { get; set; }

        [JsonProperty("substitute")]
        public SubstituteConfiguration Substitute { get; set; }

        [JsonProperty("perturb")]
        public PerturbConfiguration Perturb { get; set; }

        [JsonProperty("enablePartialAgesForRedact")]
        public bool EnablePartialAgesForRedact { get; set; } = ParameterDefaults.EnablePartialAgesForRedact;

        [JsonProperty("enablePartialDatesForRedact")]
        public bool EnablePartialDatesForRedact { get; set; } = ParameterDefaults.EnablePartialDatesForRedact;

        [JsonProperty("enablePartialZipCodesForRedact")]
        public bool EnablePartialZipCodesForRedact { get; set; } = ParameterDefaults.EnablePartialZipCodesForRedact;

        [JsonProperty("restrictedZipCodeTabulationAreas")]
        public List<string> RestrictedZipCodeTabulationAreas { get; set; }

        [JsonProperty("kAnonymity")]
        public KAnonymityParameterConfiguration KAnonymity { get; set; }

        [JsonProperty("differentialPrivacy")]
        public DifferentialPrivacyParameterConfiguration DifferentialPrivacy { get; set; }

        // ---------------------------------------------------------------------------
        // Validation
        // ---------------------------------------------------------------------------

        public void Validate()
        {
            if (DateShift != null)
            {
                ValidateDateShift(DateShift);
            }

            if (CryptoHash != null)
            {
                ValidateCryptoHash(CryptoHash);
            }

            if (Encrypt != null)
            {
                ValidateEncrypt(Encrypt);
            }
        }

        private static void ValidateDateShift(DateShiftConfiguration dateShift)
        {
            if (dateShift.DateShiftKeyPrefix != null &&
                s_dangerousPlaceholderPatterns.Contains(
                    dateShift.DateShiftKeyPrefix,
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new AnonymizerConfigurationException(
                    $"DateShift key prefix must not be one of the reserved placeholder patterns.");
            }
        }

        private static void ValidateCryptoHash(CryptoHashConfiguration cryptoHash)
        {
            if (cryptoHash.CryptoHashKey != null &&
                cryptoHash.CryptoHashKey.Length < MinCryptoHashKeyLength)
            {
                throw new AnonymizerConfigurationException(
                    $"CryptoHash key must be at least {MinCryptoHashKeyLength} bytes long.");
            }
        }

        private static void ValidateEncrypt(EncryptConfiguration encrypt)
        {
            if (encrypt.EncryptKey != null)
            {
                // Key length in bytes × 8 = bits
                int keySizeBits = encrypt.EncryptKey.Length * 8;
                if (!s_validAesKeySizeBits.Contains(keySizeBits))
                {
                    throw new AnonymizerConfigurationException(
                        $"Encrypt key size must be one of: {string.Join(", ", s_validAesKeySizeBits)} bits.");
                }
            }
        }
    }

    // -----------------------------------------------------------------------
    // Nested configuration classes
    // -----------------------------------------------------------------------

    public class KAnonymityParameterConfiguration
    {
        [JsonProperty("kValue")]
        public int KValue { get; set; } = ParameterDefaults.KValue;

        [JsonProperty("suppressionThreshold")]
        public double SuppressionThreshold { get; set; } = ParameterDefaults.SuppressionThreshold;
    }

    public class DifferentialPrivacyParameterConfiguration
    {
        [JsonProperty("epsilon")]
        public double Epsilon { get; set; } = ParameterDefaults.Epsilon;

        [JsonProperty("delta")]
        public double Delta { get; set; } = ParameterDefaults.Delta;

        [JsonProperty("sensitivity")]
        public double Sensitivity { get; set; } = ParameterDefaults.Sensitivity;

        [JsonProperty("maxCumulativeEpsilon")]
        public double MaxCumulativeEpsilon { get; set; } = ParameterDefaults.MaxCumulativeEpsilon;

        [JsonProperty("useAdvancedComposition")]
        public bool UseAdvancedComposition { get; set; } = ParameterDefaults.UseAdvancedComposition;

        [JsonProperty("mechanism")]
        public string Mechanism { get; set; } = ParameterDefaults.Mechanism;

        [JsonProperty("privacyBudgetTrackingEnabled")]
        public bool PrivacyBudgetTrackingEnabled { get; set; } = ParameterDefaults.PrivacyBudgetTrackingEnabled;

        [JsonProperty("clippingEnabled")]
        public bool ClippingEnabled { get; set; } = ParameterDefaults.ClippingEnabled;
    }
}
