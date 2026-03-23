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

        [DataMember(Name = "dateShiftKey")]
        public string DateShiftKey { get; set; }

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

        [DataMember(Name = "encryptKey")]
        public string EncryptKey { get; set; }

        [DataMember(Name = "enablePartialAgesForRedact")]
        public bool EnablePartialAgesForRedact { get; set; }

        [DataMember(Name = "enablePartialDatesForRedact")]
        public bool EnablePartialDatesForRedact { get; set; }

        [DataMember(Name = "enablePartialZipCodesForRedact")]
        public bool EnablePartialZipCodesForRedact { get; set; }

        [DataMember(Name = "restrictedZipCodeTabulationAreas")]
        public List<string> RestrictedZipCodeTabulationAreas { get; set; }

        [DataMember(Name = "kAnonymitySettings")]
        public KAnonymityParameterConfiguration KAnonymitySettings { get; set; }

        [DataMember(Name = "differentialPrivacySettings")]
        public DifferentialPrivacyParameterConfiguration DifferentialPrivacySettings { get; set; }

        [DataMember(Name = "customSettings")]
        public JObject CustomSettings { get; set; }

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
        /// based solely on the resource ID. File and Folder scopes require a user-provided key
        /// to ensure all resources in the same file/folder receive the same deterministic shift.
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
