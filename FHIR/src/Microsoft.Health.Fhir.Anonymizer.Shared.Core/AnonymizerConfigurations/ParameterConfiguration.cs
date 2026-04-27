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
    /// Top-level configuration object controlling all anonymization method parameters:
    /// date-shifting (HMAC-based and fixed-offset), cryptographic hashing, AES encryption,
    /// redaction with optional partial-data retention, k-anonymity, differential privacy,
    /// and extension settings for custom processors.
    /// </summary>
    [DataContract]
    public class ParameterConfiguration
    {
        private static readonly ILogger s_logger = AnonymizerLogging.CreateLogger<ParameterConfiguration>();

        /// <summary>
        /// Secret key for HMAC-based deterministic date shifting.
        /// Per HIPAA Safe Harbor §164.514(b)(2)(i), dates must be shifted by a consistent
        /// offset derived from this key to prevent re-identification.
        /// </summary>
        [DataMember(Name = "dateShiftKey")]
        public string DateShiftKey { get; set; }

        /// <summary>
        /// Scope at which the date-shift offset is held constant.
        /// <list type="bullet">
        ///   <item><description><see cref="DateShiftScope.Resource"/> — each resource gets an independent shift derived from its ID and key.</description></item>
        ///   <item><description><see cref="DateShiftScope.File"/> — all resources in the same file share the same shift.</description></item>
        ///   <item><description><see cref="DateShiftScope.Folder"/> — all resources in the same folder share the same shift.</description></item>
        /// </list>
        /// </summary>
        [DataMember(Name = "dateShiftScope")]
        public DateShiftScope DateShiftScope { get; set; }

        /// <summary>
        /// Optional fixed date-shift offset in days, range
        /// [<see cref="ParameterDefaults.MinDateShiftOffsetDays"/>, <see cref="ParameterDefaults.MaxDateShiftOffsetDays"/>].
        /// When null, HMAC key-based shift is used per HIPAA Safe Harbor §164.514(b)(2)(i).
        /// </summary>
        [DataMember(Name = "dateShiftFixedOffsetInDays")]
        public int? DateShiftFixedOffsetInDays { get; set; }

        /// <summary>
        /// Key for HMAC-SHA256 cryptographic hashing. Must be at least
        /// <see cref="ParameterDefaults.MinCryptoHashKeyLength"/> characters.
        /// Generate with: openssl rand -base64 32
        /// </summary>
        [DataMember(Name = "cryptoHashKey")]
        public string CryptoHashKey { get; set; }

        /// <summary>
        /// AES encryption key. Must encode to exactly 16, 24, or 32 UTF-8 bytes (AES-128/192/256).
        /// Generate with: openssl rand -base64 32
        /// </summary>
        [DataMember(Name = "encryptKey")]
        public string EncryptKey { get; set; }

        /// <summary>
        /// When true, ages 90+ are fully redacted per HIPAA Safe Harbor §164.514(b)(2)(i);
        /// ages below 90 are retained.
        /// </summary>
        [DataMember(Name = "enablePartialAgesForRedact")]
        public bool EnablePartialAgesForRedact { get; set; }

        /// <summary>When true, only the year component of a date is retained during redaction.</summary>
        [DataMember(Name = "enablePartialDatesForRedact")]
        public bool EnablePartialDatesForRedact { get; set; }

        /// <summary>
        /// When true, first three digits of ZIP code are retained unless listed in
        /// <see cref="RestrictedZipCodeTabulationAreas"/>.
        /// Aligns with HIPAA Safe Harbor §164.514(b)(2)(i).
        /// </summary>
        [DataMember(Name = "enablePartialZipCodesForRedact")]
        public bool EnablePartialZipCodesForRedact { get; set; }

        /// <summary>
        /// 3-digit ZIP code prefixes with a population under 20,000 (ZIP Code Tabulation Area
        /// threshold per HIPAA Safe Harbor §164.514(b)(2)(i)) that must be fully redacted.
        /// </summary>
        [DataMember(Name = "restrictedZipCodeTabulationAreas")]
        public List<string> RestrictedZipCodeTabulationAreas { get; set; }

        /// <summary>
        /// Optional k-anonymity post-processing configuration.
        /// When null (default), k-anonymity is disabled.
        /// </summary>
        [DataMember(Name = "kAnonymitySettings")]
        public KAnonymityParameterConfiguration KAnonymitySettings { get; set; }

        /// <summary>
        /// Optional differential privacy noise injection configuration.
        /// When null (default), differential privacy is disabled.
        /// </summary>
        [DataMember(Name = "differentialPrivacySettings")]
        public DifferentialPrivacyParameterConfiguration DifferentialPrivacySettings { get; set; }

        /// <summary>Arbitrary JSON extension settings passed through to custom processors.</summary>
        [DataMember(Name = "customSettings")]
        public JObject CustomSettings { get; set; }

        /// <summary>Optional prefix prepended to resource identifier before HMAC computation.</summary>
        public string DateShiftKeyPrefix { get; set; }

        /// <summary>
        /// Validate configuration for security issues and placeholder values.
        /// Throws <see cref="SecurityException"/> for placeholder keys (fail-secure behavior).
        /// </summary>
        public void Validate()
        {
            // SECURITY: Check for placeholder cryptographic keys
            ValidateKeyParameter(CryptoHashKey, "cryptoHashKey", "cryptographic hash");
            ValidateKeyParameter(EncryptKey, "encryptKey", "encryption");
            ValidateKeyParameter(DateShiftKey, "dateShiftKey", "date shift");

            // SECURITY: Enforce minimum length for CryptoHashKey
            if (!string.IsNullOrWhiteSpace(CryptoHashKey) && CryptoHashKey.Trim().Length < ParameterDefaults.MinCryptoHashKeyLength)
            {
                throw new SecurityException(
                    $"SECURITY ERROR: The cryptoHashKey is too short ({CryptoHashKey.Trim().Length} characters). " +
                    $"A minimum of {ParameterDefaults.MinCryptoHashKeyLength} characters is required for HMAC-SHA256. " +
                    "Generate a secure key with: openssl rand -base64 32");
            }

            // SECURITY: Validate EncryptKey is a valid AES key size (128/192/256 bits)
            ValidateEncryptKeySize(EncryptKey);

            // Validate fixed date-shift offset range
            ValidateDateShiftFixedOffsetInDays();

            // Validate DateShiftKey presence relative to DateShiftScope
            ValidateDateShiftKeyForScope();

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
        /// Validates AES key size using UTF-8 byte count to match actual runtime behavior.
        /// Valid sizes: 128, 192, or 256 bits (per NIST SP 800-57).
        /// </summary>
        private static void ValidateEncryptKeySize(string encryptKey)
        {
            if (string.IsNullOrEmpty(encryptKey))
            {
                return;
            }

            var encryptKeySize = Encoding.UTF8.GetByteCount(encryptKey) * 8;
            if (!ParameterDefaults.ValidAesKeySizeBits.Contains(encryptKeySize))
            {
                throw new AnonymizerConfigurationException(
                    $"Invalid encrypt key size : {encryptKeySize} bits! Please provide key sizes of 128, 192 or 256 bits.");
            }
        }

        /// <summary>
        /// Validates DateShiftFixedOffsetInDays is within
        /// [<see cref="ParameterDefaults.MinDateShiftOffsetDays"/>, <see cref="ParameterDefaults.MaxDateShiftOffsetDays"/>]
        /// when provided.
        /// </summary>
        private void ValidateDateShiftFixedOffsetInDays()
        {
            if (!DateShiftFixedOffsetInDays.HasValue)
            {
                return;
            }

            int offset = DateShiftFixedOffsetInDays.Value;
            if (offset < ParameterDefaults.MinDateShiftOffsetDays || offset > ParameterDefaults.MaxDateShiftOffsetDays)
            {
                throw new AnonymizerConfigurationException(
                    $"The dateShiftFixedOffsetInDays value {offset} is out of the allowed range " +
                    $"[{ParameterDefaults.MinDateShiftOffsetDays}, {ParameterDefaults.MaxDateShiftOffsetDays}]. " +
                    "Provide a value between -365 and 365 days, or omit to use key-based date shift.");
            }
        }

        /// <summary>
        /// Rejects placeholder or whitespace-only key values. SECURITY CRITICAL.
        /// </summary>
        private void ValidateKeyParameter(string keyValue, string parameterName, string keyType)
        {
            if (string.IsNullOrEmpty(keyValue))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(keyValue))
            {
                throw new SecurityException(
                    $"SECURITY ERROR: Whitespace-only {keyType} key in '{parameterName}'. " +
                    "A whitespace-only key provides no entropy. " +
                    "Generate a secure key with: openssl rand -base64 32");
            }

            var normalizedKey = keyValue.Trim().ToUpperInvariant();

            foreach (var pattern in ParameterDefaults.DangerousPlaceholderPatterns)
            {
                if (normalizedKey.Contains(pattern))
                {
                    throw new SecurityException(
                        $"SECURITY ERROR: Placeholder {keyType} key in '{parameterName}'. " +
                        $"Replace the placeholder value ('{pattern}') with a cryptographically secure key. " +
                        "Generate with: openssl rand -base64 32");
                }
            }

            if (keyValue.Length < 16)
            {
                s_logger.LogWarning(
                    $"The {keyType} key in '{parameterName}' is very short ({keyValue.Length} characters). " +
                    "Recommended minimum is 32 bytes. Short keys may be vulnerable to brute force attacks.");
            }

            if (keyValue.Equals("12345678", StringComparison.Ordinal) ||
                keyValue.Equals("password", StringComparison.OrdinalIgnoreCase) ||
                keyValue.Equals("secret", StringComparison.OrdinalIgnoreCase) ||
                keyValue.Equals("key", StringComparison.OrdinalIgnoreCase) ||
                keyValue.All(c => c == keyValue[0]))
            {
                throw new SecurityException(
                    $"SECURITY ERROR: Weak {keyType} key in '{parameterName}'. " +
                    "The key is a common weak value (e.g., 'password', repeated characters). " +
                    "Generate a secure key with: openssl rand -base64 32");
            }
        }

        /// <summary>
        /// Validates that DateShiftKey is present when DateShiftFixedOffsetInDays is not set.
        /// Without a key, the HMAC shift depends only on the predictable resource ID,
        /// enabling re-identification attacks.
        /// </summary>
        private void ValidateDateShiftKeyForScope()
        {
            var scope = this.DateShiftScope;

            if (string.IsNullOrEmpty(DateShiftKey) && !DateShiftFixedOffsetInDays.HasValue)
            {
                throw new AnonymizerConfigurationException(
                    $"A dateShiftKey is required when dateShiftScope is '{scope}' and dateShiftFixedOffsetInDays is not set. " +
                    "Provide a non-empty dateShiftKey, or set dateShiftFixedOffsetInDays.");
            }
        }

        /// <summary>
        /// Validates differential privacy parameters per NIST SP 800-188:
        /// epsilon in (0, 10], delta in [0, 1], sensitivity &gt; 0, maxCumulativeEpsilon &gt; 0.
        /// </summary>
        private void ValidateDifferentialPrivacySettings(DifferentialPrivacyParameterConfiguration settings)
        {
            if (settings.Epsilon <= 0)
            {
                throw new ArgumentException("Differential privacy epsilon must be greater than 0");
            }

            if (settings.Epsilon > 10.0)
            {
                throw new ArgumentException(
                    $"Differential privacy epsilon value {settings.Epsilon} exceeds maximum of 10.0. " +
                    "High epsilon values provide minimal privacy protection.");
            }

            // Warn only when epsilon is strictly above 1.0. The default epsilon IS 1.0, so
            // the previous condition (>= 1.0) caused spurious LogWarning on every default instance.
            if (settings.Epsilon > 1.0)
            {
                s_logger.LogWarning(
                    $"Differential privacy epsilon value {settings.Epsilon} is high (>1.0). " +
                    "Consider epsilon <= 0.1 for strong privacy (NIST SP 800-188 for health data).");
            }

            if (settings.Delta < 0 || settings.Delta > 1)
            {
                throw new ArgumentException("Differential privacy delta must be between 0 and 1");
            }

            if (settings.Sensitivity <= 0)
            {
                throw new ArgumentException("Differential privacy sensitivity must be greater than 0");
            }

            if (settings.MaxCumulativeEpsilon <= 0)
            {
                throw new ArgumentException("Differential privacy maxCumulativeEpsilon must be greater than 0");
            }
        }

        /// <summary>
        /// Validates k-anonymity parameters. k must be &gt;= 2 (k=1 provides no protection).
        /// k=2 is permitted but triggers a warning; HIPAA Safe Harbor recommends k &gt;= 5.
        /// Privacy composition theorem: combining k-anonymity with l-diversity or t-closeness
        /// provides stronger privacy guarantees.
        /// </summary>
        private void ValidateKAnonymitySettings(KAnonymityParameterConfiguration settings)
        {
            if (settings.KValue < 2)
            {
                throw new ArgumentException(
                    $"K-anonymity k-value must be at least 2 (provided: {settings.KValue}). " +
                    "k=1 provides no privacy protection.");
            }

            if (settings.KValue == 2)
            {
                s_logger.LogWarning(
                    "K-anonymity k-value is 2 (minimal). Consider k >= 5 (HIPAA Safe Harbor guidance).");
            }

            if (settings.SuppressionThreshold < 0 || settings.SuppressionThreshold > 1)
            {
                throw new ArgumentException(
                    "K-anonymity suppression threshold must be between 0 and 1");
            }
        }
    }

    /// <summary>Configuration parameters for k-anonymity processing.</summary>
    [DataContract]
    public class KAnonymityParameterConfiguration
    {
        /// <summary>
        /// Minimum group size (default: 5). HIPAA Safe Harbor guidance recommends k &gt;= 5
        /// to ensure each record is indistinguishable from at least 4 others.
        /// </summary>
        [DataMember(Name = "kValue")]
        public int KValue { get; set; } = 5;

        /// <summary>
        /// FHIR paths to quasi-identifiers used in equivalence class partitioning.
        /// Example: ["Patient.birthDate", "Patient.address.postalCode", "Patient.gender"]
        /// </summary>
        [DataMember(Name = "quasiIdentifiers")]
        public List<string> QuasiIdentifiers { get; set; }

        /// <summary>Generalization hierarchies: maps FHIR path to generalization strategy.</summary>
        [DataMember(Name = "generalizationHierarchies")]
        public Dictionary<string, object> GeneralizationHierarchies { get; set; }

        /// <summary>
        /// Suppression threshold 0.0–1.0 (default: 0.3). Records that cannot be generalized
        /// into a k-group and exceed this fraction of the dataset are suppressed.
        /// </summary>
        [DataMember(Name = "suppressionThreshold")]
        public double SuppressionThreshold { get; set; } = 0.3;
    }

    /// <summary>
    /// Configuration parameters for differential privacy processing.
    /// References: NIST SP 800-188; Dwork and Roth (2014) "Algorithmic Foundations of Differential Privacy".
    /// The privacy composition theorem applies: sequential composition of k mechanisms each with
    /// epsilon-DP yields k*epsilon-DP overall.
    /// </summary>
    [DataContract]
    public class DifferentialPrivacyParameterConfiguration
    {
        /// <summary>
        /// Privacy budget epsilon (default: 1.0). Lower = stronger privacy.
        /// Per NIST SP 800-188: epsilon &lt;= 0.1 strong, 0.5–1.0 moderate, 1.0–10.0 weak.
        /// </summary>
        [DataMember(Name = "epsilon")]
        public double Epsilon { get; set; } = 1.0;

        /// <summary>
        /// Delta for (epsilon,delta)-DP (default: 1e-5). Probability of privacy failure.
        /// Should be much smaller than 1/n; typical values 1e-5 to 1e-8 for healthcare.
        /// </summary>
        [DataMember(Name = "delta")]
        public double Delta { get; set; } = 1e-5;

        /// <summary>
        /// Global sensitivity of the query function (default: 1.0).
        /// Counting queries: 1; sum queries: max value; average: range/n.
        /// </summary>
        [DataMember(Name = "sensitivity")]
        public double Sensitivity { get; set; } = 1.0;

        /// <summary>
        /// Maximum cumulative epsilon before warning (default: 1.0).
        /// Used for privacy budget tracking via the composition theorem.
        /// </summary>
        [DataMember(Name = "maxCumulativeEpsilon")]
        public double MaxCumulativeEpsilon { get; set; } = 1.0;

        /// <summary>
        /// Use advanced composition for tighter privacy accounting per NIST SP 800-188 §4.3.
        /// Not yet implemented; falls back to sequential composition. Default: false.
        /// </summary>
        [DataMember(Name = "useAdvancedComposition")]
        public bool UseAdvancedComposition { get; set; } = false;

        /// <summary>
        /// Noise mechanism: "laplace" (epsilon-DP), "gaussian" ((epsilon,delta)-DP), "exponential".
        /// Default: "laplace".
        /// </summary>
        [DataMember(Name = "mechanism")]
        public string Mechanism { get; set; } = "laplace";

        /// <summary>
        /// When true, tracks cumulative epsilon and warns when
        /// <see cref="MaxCumulativeEpsilon"/> is exceeded. Default: false.
        /// </summary>
        [DataMember(Name = "privacyBudgetTrackingEnabled")]
        public bool PrivacyBudgetTrackingEnabled { get; set; } = false;

        /// <summary>
        /// When true, clips input values before adding noise to bound sensitivity.
        /// Default: false.
        /// </summary>
        [DataMember(Name = "clippingEnabled")]
        public bool ClippingEnabled { get; set; } = false;
    }
}
