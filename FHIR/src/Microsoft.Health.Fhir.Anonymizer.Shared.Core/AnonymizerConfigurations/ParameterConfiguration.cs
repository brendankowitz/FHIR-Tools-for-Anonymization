using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    /// <summary>
    /// Top-level configuration object that controls all anonymization method parameters.
    /// Covers date-shifting (HMAC-based and fixed-offset), cryptographic hashing, AES encryption,
    /// redaction (with optional partial-data retention for ages, dates, and ZIP codes),
    /// k-anonymity post-processing, differential privacy noise injection, and arbitrary
    /// extension settings for custom processors.
    /// </summary>
    [DataContract]
    public class ParameterConfiguration
    {
        private static readonly ILogger s_logger = AnonymizerLogging.CreateLogger<ParameterConfiguration>();

        /// <summary>
        /// Minimum allowed value for <see cref="DateShiftFixedOffsetInDays"/> (inclusive).
        /// </summary>
        public const int MinDateShiftOffsetDays = -365;

        /// <summary>
        /// Maximum allowed value for <see cref="DateShiftFixedOffsetInDays"/> (inclusive).
        /// </summary>
        public const int MaxDateShiftOffsetDays = 365;

        /// <summary>
        /// Minimum required length (in characters) for <see cref="CryptoHashKey"/>.
        /// Keys shorter than this value do not provide adequate entropy for HMAC-SHA256.
        /// </summary>
        public const int MinCryptoHashKeyLength = 32;

        /// <summary>
        /// Secret key used for HMAC-based deterministic date shifting.
        /// Combined with the resource, file, or folder identifier (according to
        /// <see cref="DateShiftScope"/>) to produce a consistent, reproducible date offset
        /// for each unique identifier. Must not be a placeholder or whitespace-only value.
        /// </summary>
        [DataMember(Name = "dateShiftKey")]
        public string DateShiftKey { get; set; }

        /// <summary>
        /// Granularity scope at which the date-shift offset is held constant.
        /// <list type="bullet">
        ///   <item><description><c>Resource</c> – each resource receives its own deterministic offset derived from its ID and <see cref="DateShiftKey"/>.</description></item>
        ///   <item><description><c>File</c> – all resources in the same input file share a single offset.</description></item>
        ///   <item><description><c>Folder</c> – all resources in the same folder share a single offset.</description></item>
        /// </list>
        /// Narrower scopes (Resource) maximise per-record randomness; wider scopes (Folder)
        /// preserve temporal relationships across records processed together.
        /// </summary>
        [DataMember(Name = "dateShiftScope")]
        public DateShiftScope DateShiftScope { get; set; }

        /// <summary>
        /// Optional fixed date-shift offset in days. When set, overrides the deterministic
        /// key-based date shift. Must be in the range [<see cref="MinDateShiftOffsetDays"/>,
        /// <see cref="MaxDateShiftOffsetDays"/>] (i.e. -365 to +365). When null the
        /// cryptographic key-based shift is used instead.
        /// </summary>
        [DataMember(Name = "dateShiftFixedOffsetInDays")]
        public int? DateShiftFixedOffsetInDays { get; set; }

        /// <summary>
        /// Key used for HMAC-SHA256 cryptographic hashing of identifiers.
        /// Must be ≥ <see cref="MinCryptoHashKeyLength"/> characters (non-whitespace) to ensure
        /// adequate entropy. Whitespace-only values are rejected. Generate a secure key using:
        ///   openssl rand -base64 32
        /// </summary>
        [DataMember(Name = "cryptoHashKey")]
        public string CryptoHashKey { get; set; }

        /// <summary>
        /// AES symmetric encryption key used by the <c>encrypt</c> anonymization method.
        /// The key must encode to exactly 16, 24, or 32 UTF-8 bytes, corresponding to
        /// AES-128, AES-192, and AES-256 respectively. Keys of any other length are
        /// rejected during <see cref="Validate"/>. Generate a 256-bit key with:
        ///   openssl rand -base64 32
        /// </summary>
        [DataMember(Name = "encryptKey")]
        public string EncryptKey { get; set; }

        /// <summary>
        /// When <see langword="true"/>, ages 90 and above are fully redacted while ages
        /// below 90 are retained as-is, following the HIPAA Safe Harbor de-identification
        /// standard which treats ages ≥ 90 as a direct identifier.
        /// When <see langword="false"/> (default), all age values are redacted.
        /// </summary>
        [DataMember(Name = "enablePartialAgesForRedact")]
        public bool EnablePartialAgesForRedact { get; set; }

        /// <summary>
        /// When <see langword="true"/>, only the year component of a date value is
        /// retained during redaction; month and day are removed. This preserves limited
        /// temporal utility while reducing re-identification risk.
        /// When <see langword="false"/> (default), date values are fully redacted.
        /// </summary>
        [DataMember(Name = "enablePartialDatesForRedact")]
        public bool EnablePartialDatesForRedact { get; set; }

        /// <summary>
        /// When <see langword="true"/>, the first three digits of a ZIP code are retained
        /// during redaction, unless the prefix appears in
        /// <see cref="RestrictedZipCodeTabulationAreas"/>, in which case the entire ZIP
        /// code is redacted. This aligns with HIPAA Safe Harbor, which permits the
        /// 3-digit prefix for geographic areas with a population ≥ 20,000.
        /// When <see langword="false"/> (default), ZIP codes are fully redacted.
        /// </summary>
        [DataMember(Name = "enablePartialZipCodesForRedact")]
        public bool EnablePartialZipCodesForRedact { get; set; }

        /// <summary>
        /// List of 3-digit ZIP code prefixes (ZIP Code Tabulation Areas) that must be
        /// fully redacted because the corresponding geographic area has a population of
        /// fewer than 20,000 people, per HIPAA Safe Harbor §164.514(b)(2)(i).
        /// Only evaluated when <see cref="EnablePartialZipCodesForRedact"/> is
        /// <see langword="true"/>.
        /// </summary>
        [DataMember(Name = "restrictedZipCodeTabulationAreas")]
        public List<string> RestrictedZipCodeTabulationAreas { get; set; }

        /// <summary>
        /// Optional configuration for k-anonymity post-processing. When set, the engine
        /// enforces that every combination of quasi-identifier values appears in at least
        /// <see cref="KAnonymityParameterConfiguration.KValue"/> records, suppressing or
        /// generalizing records that cannot satisfy the constraint.
        /// When <see langword="null"/> (default), k-anonymity post-processing is disabled.
        /// </summary>
        [DataMember(Name = "kAnonymitySettings")]
        public KAnonymityParameterConfiguration KAnonymitySettings { get; set; }

        /// <summary>
        /// Optional configuration for differential privacy noise injection. When set, the
        /// engine adds calibrated random noise to numeric fields according to the specified
        /// <see cref="DifferentialPrivacyParameterConfiguration.Epsilon"/> and
        /// <see cref="DifferentialPrivacyParameterConfiguration.Mechanism"/> parameters.
        /// When <see langword="null"/> (default), differential privacy is disabled.
        /// </summary>
        [DataMember(Name = "differentialPrivacySettings")]
        public DifferentialPrivacyParameterConfiguration DifferentialPrivacySettings { get; set; }

        /// <summary>
        /// Extension point for tool-specific or experimental settings, stored as an
        /// arbitrary JSON object. The anonymizer engine does not interpret this field;
        /// it is passed through as-is to custom processors that may inspect it.
        /// Use this to attach metadata or feature flags without modifying the core schema.
        /// </summary>
        [DataMember(Name = "customSettings")]
        public JObject CustomSettings { get; set; }

        /// <summary>
        /// Optional prefix prepended to the resource (or file/folder) identifier before
        /// HMAC computation during date shifting. Useful for namespace isolation when the
        /// same <see cref="DateShiftKey"/> is reused across multiple datasets: setting a
        /// distinct prefix per dataset ensures that identical resource IDs in different
        /// datasets produce different date-shift offsets.
        /// </summary>
        public string DateShiftKeyPrefix { get; set; }

        /// <summary>
        /// Validate configuration for security issues and placeholder values.
        ///
        /// SECURITY: Rejects dangerous placeholder values that should never be used in production.
        /// This prevents accidental use of example/template configurations with insecure dummy keys.
        /// Throws SecurityException for placeholder keys to ensure fail-secure behavior.
        /// </summary>
        public void Validate()
        {
            // SECURITY: Check for placeholder cryptographic keys
            CryptographicKeyValidator.ValidateKeyParameter(CryptoHashKey, "cryptoHashKey", "cryptographic hash");
            CryptographicKeyValidator.ValidateKeyParameter(EncryptKey, "encryptKey", "encryption");
            CryptographicKeyValidator.ValidateKeyParameter(DateShiftKey, "dateShiftKey", "date shift");

            // SECURITY: Enforce minimum length for CryptoHashKey
            if (!string.IsNullOrWhiteSpace(CryptoHashKey) && CryptoHashKey.Trim().Length < MinCryptoHashKeyLength)
            {
                throw new SecurityException(
                    $"SECURITY ERROR: The cryptoHashKey is too short ({CryptoHashKey.Trim().Length} characters). " +
                    $"A minimum of {MinCryptoHashKeyLength} characters is required to ensure adequate entropy for " +
                    "HMAC-SHA256 operations.\n\n" +
                    "TO GENERATE A SECURE KEY:\n" +
                    "  Linux/macOS:   openssl rand -base64 32\n" +
                    "  Windows:       pwsh -Command \"[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))\"\n" +
                    "  .NET:          var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));");
            }

            // SECURITY: Validate EncryptKey is a valid AES key size (128/192/256 bits)
            CryptographicKeyValidator.ValidateEncryptKeySize(EncryptKey);

            // Validate fixed date-shift offset range
            ValidateDateShiftFixedOffsetInDays();

            // Validate DateShiftKey presence relative to DateShiftScope
            ValidateDateShiftKeyForScope();

            // Validate differential privacy settings
            DifferentialPrivacySettings?.Validate();

            // Validate k-anonymity settings
            KAnonymitySettings?.Validate();
        }

        /// <summary>
        /// Validate that <see cref="DateShiftFixedOffsetInDays"/>, when provided, falls within
        /// the allowed range [<see cref="MinDateShiftOffsetDays"/>, <see cref="MaxDateShiftOffsetDays"/>].
        /// A null value is always valid — it simply means the key-based shift will be used.
        /// </summary>
        private void ValidateDateShiftFixedOffsetInDays()
        {
            if (!DateShiftFixedOffsetInDays.HasValue)
            {
                return;
            }

            int offset = DateShiftFixedOffsetInDays.Value;
            if (offset < MinDateShiftOffsetDays || offset > MaxDateShiftOffsetDays)
            {
                throw new AnonymizerConfigurationException(
                    $"The dateShiftFixedOffsetInDays value {offset} is out of the allowed range " +
                    $"[{MinDateShiftOffsetDays}, {MaxDateShiftOffsetDays}]. " +
                    "Provide a value between -365 and 365 days, or omit the setting to use the " +
                    "deterministic key-based date shift.");
            }
        }

        /// <summary>
        /// Validate that a non-empty DateShiftKey is present for File and Folder scopes
        /// when DateShiftFixedOffsetInDays is not set.
        ///
        /// Resource scope allows an auto-generated key because each resource shifts independently
        /// based on the resource ID. File and Folder scopes require a user-provided key to ensure
        /// all resources in the same file or folder receive the same deterministic shift.
        /// </summary>
        private void ValidateDateShiftKeyForScope()
        {
            // Use a local variable to avoid ambiguity between the property name and the enum type name.
            var scope = this.DateShiftScope;

            // Resource scope permits an auto-generated key (each resource shifts independently)
            if (scope == DateShiftScope.Resource)
            {
                return;
            }

            // File and Folder scopes require a deterministic key for consistent shifts
            if (string.IsNullOrEmpty(DateShiftKey) && !DateShiftFixedOffsetInDays.HasValue)
            {
                throw new AnonymizerConfigurationException(
                    $"A dateShiftKey is required when dateShiftScope is '{scope}' and dateShiftFixedOffsetInDays is not set. " +
                    "Provide a non-empty dateShiftKey, or set dateShiftFixedOffsetInDays to use a fixed date-shift offset instead.");
            }
        }
    }
}
