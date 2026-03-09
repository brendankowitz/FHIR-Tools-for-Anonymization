using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security;
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
        /// Minimum required length for cryptographic keys (32 characters / 256 bits).
        ///
        /// RATIONALE (NIST SP 800-107):
        /// HMAC-SHA256 produces a 256-bit (32-byte) output. Per NIST recommendation,
        /// the HMAC key should be at least as long as the hash output length to ensure
        /// full security strength. Keys shorter than 32 bytes reduce the effective
        /// security of the HMAC operation below 256 bits.
        ///
        /// Reference: NIST SP 800-107 Rev. 1, Section 5.3.4
        /// </summary>
        internal const int MinimumKeyLength = 32;

        /// <summary>
        /// Dangerous placeholder patterns that must be rejected
        /// </summary>
        private static readonly string[] s_dangerousPlaceholderPatterns = new[]
        {
            "$HMAC_KEY",
            "YOUR_KEY_HERE",
            "YOUR_SECURE_KEY",
            "YOUR_ENCRYPTION_KEY",
            "PLACEHOLDER",
            "CHANGE_ME",
            "CHANGEME",
            "REPLACE_ME",
            "EXAMPLE_KEY",
            "TEST_KEY",
            "SAMPLE_KEY",
            "INSERT_KEY_HERE",
            "<YOUR_KEY>",
            "[YOUR_KEY]",
            "{{YOUR_KEY}}",
            "TODO",
            "FIXME"
        };

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
        /// The secret key used for HMAC-SHA256 cryptographic hashing operations.
        ///
        /// REQUIREMENTS:
        /// - Must be at least <see cref="MinimumKeyLength"/> characters (assuming ASCII/Base64
        ///   encoding, equivalent to 32 bytes).
        /// - Must not be null, empty, or whitespace-only when the cryptoHash method is used.
        /// - Must not be a placeholder or obviously weak value.
        ///
        /// SECURITY (NIST SP 800-107):
        /// HMAC-SHA256 uses this key to produce a keyed hash. A key shorter than 32 bytes
        /// reduces the effective security strength below 256 bits and may be vulnerable to
        /// brute-force attacks. Use a cryptographically random key of at least 32 bytes.
        ///
        /// KEY GENERATION COMMANDS:
        ///   Linux/macOS:   openssl rand -base64 32
        ///   Windows:       pwsh -Command "[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))"
        ///   .NET:          var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        ///
        /// See also: NIST SP 800-107 Rev. 1, https://csrc.nist.gov/publications/detail/sp/800-107/rev-1/final
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
            ValidateKeyParameter(CryptoHashKey, "cryptoHashKey", "cryptographic hash");
            ValidateKeyParameter(EncryptKey, "encryptKey", "encryption");
            ValidateKeyParameter(DateShiftKey, "dateShiftKey", "date shift");

            // Validate fixed date-shift offset range
            ValidateDateShiftFixedOffsetInDays();

            // Validate differential privacy settings
            if (DifferentialPrivacySettings != null)
            {
                ValidateDifferentialPrivacySettings(DifferentialPrivacySettings);
            }

            // Validate k-anonymity settings
            if (KAnonymitySettings != null)
            {
                ValidateKAnonymitySettings(KAnonymitySettings);
            }
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
        /// Validate a key parameter doesn't contain placeholder values.
        /// SECURITY CRITICAL: Prevents use of example/template keys in production.
        /// </summary>
        private void ValidateKeyParameter(string keyValue, string parameterName, string keyType)
        {
            if (string.IsNullOrEmpty(keyValue))
            {
                return; // Empty keys are allowed if the feature is not used
            }

            // At this point keyValue is guaranteed to be non-null and non-empty;
            // the IsNullOrWhiteSpace check below only fires for whitespace-only strings.
            // SECURITY: Reject whitespace-only keys; they provide no entropy
            if (string.IsNullOrWhiteSpace(keyValue))
            {
                throw new SecurityException(
                    $"SECURITY ERROR: Whitespace-only {keyType} key detected in '{parameterName}'. " +
                    "A key consisting only of whitespace characters provides no cryptographic security. " +
                    "Generate a cryptographically secure random key using: openssl rand -base64 32");
            }

            // Trim and convert to uppercase for case-insensitive comparison
            var normalizedKey = keyValue.Trim().ToUpperInvariant();

            // Check against all dangerous placeholder patterns
            foreach (var pattern in s_dangerousPlaceholderPatterns)
            {
                if (normalizedKey.Contains(pattern))
                {
                    throw new SecurityException(
                        $"SECURITY ERROR: Placeholder {keyType} key detected in '{parameterName}'.\n\n" +
                        $"The configuration contains a placeholder value ('{pattern}') that must be replaced " +
                        "with a cryptographically secure key before use.\n\n" +
                        "TO GENERATE A SECURE KEY:\n" +
                        "  Linux/macOS:   openssl rand -base64 32\n" +
                        "  Windows:       pwsh -Command \"[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))\"\n" +
                        "  .NET:          var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));\n\n" +
                        "SECURITY WARNING: Using placeholder keys in production:\n" +
                        "  - Compromises cryptographic operations\n" +
                        "  - May lead to predictable hash values\n" +
                        "  - Enables re-identification attacks\n" +
                        "  - Violates privacy guarantees\n\n" +
                        "BEST PRACTICES:\n" +
                        "  - Never commit actual keys to version control\n" +
                        "  - Use environment variables: Environment.GetEnvironmentVariable(\"CRYPTO_KEY\")\n" +
                        "  - Use Azure Key Vault, AWS Secrets Manager, or similar for production\n" +
                        "  - Rotate keys periodically according to your security policy\n" +
                        "  - Use different keys for different environments (dev/staging/production)\n");
                }
            }

            // SECURITY: Enforce minimum key length (NIST SP 800-107: HMAC key >= hash output length)
            // SHA-256 output = 32 bytes; therefore minimum key length is 32 characters (MinimumKeyLength).
            if (keyValue.Length < MinimumKeyLength)
            {
                throw new SecurityException(
                    $"SECURITY ERROR: The {keyType} key in '{parameterName}' is too short " +
                    $"({keyValue.Length} characters). The minimum required length is {MinimumKeyLength} characters " +
                    $"({MinimumKeyLength} bytes / 256 bits) per NIST SP 800-107 Rev. 1 guidance that HMAC keys " +
                    "should be at least as long as the hash output length (SHA-256 = 32 bytes).\n\n" +
                    "TO GENERATE A SECURE KEY:\n" +
                    "  Linux/macOS:   openssl rand -base64 32\n" +
                    "  Windows:       pwsh -Command \"[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))\"\n" +
                    "  .NET:          var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));");
            }

            // Check for obviously weak patterns
            if (keyValue.Equals("12345678", StringComparison.Ordinal) ||
                keyValue.Equals("password", StringComparison.OrdinalIgnoreCase) ||
                keyValue.Equals("secret", StringComparison.OrdinalIgnoreCase) ||
                keyValue.Equals("key", StringComparison.OrdinalIgnoreCase) ||
                keyValue.All(c => c == keyValue[0])) // All same character
            {
                throw new SecurityException(
                    $"SECURITY ERROR: Weak {keyType} key detected in '{parameterName}'. " +
                    "The key appears to be a common weak value (e.g., 'password', '12345678', repeated characters). " +
                    "Generate a cryptographically secure random key using: openssl rand -base64 32");
            }
        }

        private void ValidateDifferentialPrivacySettings(DifferentialPrivacyParameterConfiguration settings)
        {
            if (settings.Epsilon <= 0)
                throw new ArgumentException("Differential privacy epsilon must be greater than 0");
            if (settings.Epsilon > 10.0)
                throw new ArgumentException($"Differential privacy epsilon value {settings.Epsilon} exceeds maximum of 10.0. High epsilon values provide minimal privacy protection. See configuration comments for guidance.");
            if (settings.Epsilon > 1.0)
                s_logger.LogWarning($"Differential privacy epsilon value {settings.Epsilon} is high (>1.0). This provides weaker privacy guarantees. Consider using epsilon ≤ 1.0 for moderate privacy or epsilon ≤ 0.1 for strong privacy (NIST SP 800-188 guidance for health data).");
            if (settings.Delta < 0 || settings.Delta > 1)
                throw new ArgumentException("Differential privacy delta must be between 0 and 1");
            if (settings.Sensitivity <= 0)
                throw new ArgumentException("Differential privacy sensitivity must be greater than 0");
            if (settings.MaxCumulativeEpsilon <= 0)
                throw new ArgumentException("Differential privacy maxCumulativeEpsilon must be greater than 0");
        }

        private void ValidateKAnonymitySettings(KAnonymityParameterConfiguration settings)
        {
            if (settings.KValue < 2)
                throw new ArgumentException($"K-anonymity k-value must be at least 2 (provided: {settings.KValue}). k=1 provides no privacy protection.");
            if (settings.KValue == 2)
                s_logger.LogWarning("K-anonymity k-value is 2 (minimal). Consider k ≥ 5 for better privacy protection (recommended by HIPAA Safe Harbor guidance).");
            if (settings.SuppressionThreshold < 0 || settings.SuppressionThreshold > 1)
                throw new ArgumentException("K-anonymity suppression threshold must be between 0 and 1 (represents percentage)");
        }
    }

    [DataContract]
    public class KAnonymityParameterConfiguration
    {
        [DataMember(Name = "kValue")]
        public int KValue { get; set; } = 5;

        [DataMember(Name = "quasiIdentifiers")]
        public List<string> QuasiIdentifiers { get; set; }

        [DataMember(Name = "generalizationHierarchies")]
        public Dictionary<string, JObject> GeneralizationHierarchies { get; set; }

        [DataMember(Name = "suppressionThreshold")]
        public double SuppressionThreshold { get; set; } = 0.3;
    }

    [DataContract]
    public class DifferentialPrivacyParameterConfiguration
    {
        [DataMember(Name = "epsilon")]
        public double Epsilon { get; set; } = 0.1;

        [DataMember(Name = "delta")]
        public double Delta { get; set; } = 1e-5;

        [DataMember(Name = "sensitivity")]
        public double Sensitivity { get; set; } = 1.0;

        [DataMember(Name = "mechanism")]
        public string Mechanism { get; set; } = "laplace";

        [DataMember(Name = "maxCumulativeEpsilon")]
        public double MaxCumulativeEpsilon { get; set; } = 1.0;

        [DataMember(Name = "useAdvancedComposition")]
        public bool UseAdvancedComposition { get; set; } = false;
    }
}
