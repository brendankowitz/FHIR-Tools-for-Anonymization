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
        /// Valid AES key sizes in bits. Used to validate EncryptKey without allocating an Aes instance.
        /// AES supports 128-bit (16 bytes), 192-bit (24 bytes), and 256-bit (32 bytes) keys.
        /// </summary>
        private static readonly HashSet<int> s_validAesKeySizeBits = new HashSet<int> { 128, 192, 256 };

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
            ValidateKeyParameter(CryptoHashKey, "cryptoHashKey", "cryptographic hash");
            ValidateKeyParameter(EncryptKey, "encryptKey", "encryption");
            ValidateKeyParameter(DateShiftKey, "dateShiftKey", "date shift");

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
        /// Validate that the encrypt key size is a valid AES key size (128, 192, or 256 bits).
        /// Uses a static HashSet of valid sizes to avoid allocating an Aes instance on every call.
        /// Only validates when encryptKey is non-null and non-empty.
        /// </summary>
        private static void ValidateEncryptKeySize(string encryptKey)
        {
            if (string.IsNullOrEmpty(encryptKey))
            {
                return;
            }

            var encryptKeySize = Encoding.UTF8.GetByteCount(encryptKey) * 8;
            if (!s_validAesKeySizeBits.Contains(encryptKeySize))
            {
                throw new AnonymizerConfigurationException(
                    $"Invalid encrypt key size : {encryptKeySize} bits! Please provide key sizes of 128, 192 or 256 bits.");
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
        /// Validate a key parameter doesn't contain placeholder values or consist solely of whitespace.
        /// SECURITY CRITICAL: Prevents use of example/template keys and whitespace-only values in production.
        /// </summary>
        private void ValidateKeyParameter(string keyValue, string parameterName, string keyType)
        {
            if (string.IsNullOrEmpty(keyValue))
            {
                return; // Empty/null keys are allowed if the feature is not used
            }

            // SECURITY: Reject whitespace-only keys — they provide no entropy
            if (string.IsNullOrWhiteSpace(keyValue))
            {
                throw new SecurityException(
                    $"SECURITY ERROR: Whitespace-only {keyType} key detected in '{parameterName}'. " +
                    "A key consisting entirely of whitespace characters provides no entropy and must not be used. " +
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
                        "  Windows:       pwsh -Command \"[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))\"\n" +
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

            // Additional checks for weak or test keys
            if (keyValue.Length < 16)
            {
                s_logger.LogWarning(
                    $"The {keyType} key in '{parameterName}' is very short ({keyValue.Length} characters). " +
                    "Recommended minimum is 32 bytes (44 characters in Base64). " +
                    "Short keys provide inadequate security and may be vulnerable to brute force attacks.");
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

        /// <summary>
        /// Validate that a non-empty DateShiftKey is present for ALL DateShiftScope values
        /// (Resource, File, and Folder) when DateShiftFixedOffsetInDays is not set.
        ///
        /// SECURITY: Resource scope also requires a key because the HMAC-based date shift uses
        /// (resourceId + dateShiftKey) as its input. Without a key, the shift is determined solely
        /// by the resource ID, which is often predictable or publicly known (e.g., in FHIR bundles
        /// or EHR systems). An attacker who knows the resource ID can recompute the shift and
        /// reverse the date offset — enabling re-identification. A secret key prevents this.
        ///
        /// File and Folder scopes additionally require a key for consistency: all resources
        /// in the same file or folder must receive the same deterministic shift.
        /// </summary>
        private void ValidateDateShiftKeyForScope()
        {
            // Use a local variable to avoid ambiguity between the property name and the enum type name.
            var scope = this.DateShiftScope;

            if (string.IsNullOrEmpty(DateShiftKey) &&
                !DateShiftFixedOffsetInDays.HasValue)
            {
                throw new AnonymizerConfigurationException(
                    $"A dateShiftKey is required when dateShiftScope is '{scope}' and dateShiftFixedOffsetInDays is not set. " +
                    "Provide a non-empty dateShiftKey, or set dateShiftFixedOffsetInDays to use a fixed date-shift offset instead.");
            }
        }

        /// <summary>
        /// Validate differential privacy configuration parameters
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
                    "High epsilon values provide minimal privacy protection. See configuration comments for guidance.");
            }

            if (settings.Epsilon > 1.0)
            {
                s_logger.LogWarning(
                    $"Differential privacy epsilon value {settings.Epsilon} is high (>1.0). " +
                    "This provides weaker privacy guarantees. Consider using epsilon <= 1.0 for moderate privacy " +
                    "or epsilon <= 0.1 for strong privacy (NIST SP 800-188 guidance for health data).");
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
        /// Validate k-anonymity configuration parameters
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
                    "K-anonymity k-value is 2 (minimal). Consider k >= 5 for better privacy protection " +
                    "(recommended by HIPAA Safe Harbor guidance).");
            }

            if (settings.SuppressionThreshold < 0 || settings.SuppressionThreshold > 1)
            {
                throw new ArgumentException(
                    "K-anonymity suppression threshold must be between 0 and 1 (represents percentage)");
            }
        }
    }

    /// <summary>
    /// Configuration parameters for k-anonymity processing
    /// </summary>
    [DataContract]
    public class KAnonymityParameterConfiguration
    {
        /// <summary>
        /// Minimum group size for k-anonymity (default: 5)
        /// Each combination of quasi-identifiers must appear in at least k records.
        /// Higher values provide stronger privacy but may require more aggressive generalization.
        /// </summary>
        [DataMember(Name = "kValue")]
        public int KValue { get; set; } = 5;

        /// <summary>
        /// List of FHIR paths to quasi-identifiers.
        /// Quasi-identifiers are attributes that together could identify individuals.
        /// Example: ["Patient.birthDate", "Patient.address.postalCode", "Patient.gender"]
        /// </summary>
        [DataMember(Name = "quasiIdentifiers")]
        public List<string> QuasiIdentifiers { get; set; }

        /// <summary>
        /// Generalization hierarchies for quasi-identifiers (optional).
        /// Maps FHIR path to generalization strategy configuration.
        /// Defines how values should be generalized to achieve k-anonymity.
        /// </summary>
        [DataMember(Name = "generalizationHierarchies")]
        public Dictionary<string, object> GeneralizationHierarchies { get; set; }

        /// <summary>
        /// Suppression threshold (0.0-1.0). Records that cannot be generalized to meet
        /// k-anonymity within this fraction of the dataset will be suppressed (removed).
        /// Default: 0.3 (30%). High suppression rates indicate data utility loss.
        /// </summary>
        [DataMember(Name = "suppressionThreshold")]
        public double SuppressionThreshold { get; set; } = 0.3;
    }

    /// <summary>
    /// Configuration parameters for differential privacy processing.
    ///
    /// REFERENCES:
    /// - NIST Special Publication 800-188: "De-Identifying Government Datasets" (2023 Draft)
    ///   https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-188-draft2.pdf
    /// - Dwork, C., &amp; Roth, A. (2014). "The Algorithmic Foundations of Differential Privacy."
    ///   Foundations and Trends in Theoretical Computer Science, 9(3-4), 211-407.
    /// - Apple Differential Privacy Team (2017). "Learning with Privacy at Scale."
    ///   Apple Machine Learning Journal, Vol. 1, Issue 8.
    /// </summary>
    [DataContract]
    public class DifferentialPrivacyParameterConfiguration
    {
        /// <summary>
        /// Privacy budget (epsilon) - lower values provide stronger privacy.
        ///
        /// GUIDANCE (NIST SP 800-188):
        /// - ε ≤ 0.1: Strong privacy protection (recommended for sensitive health data)
        /// - ε = 0.5-1.0: Moderate privacy (reasonable for many applications)
        /// - ε = 1.0-10.0: Weak privacy (use only when data utility is critical)
        /// - ε > 10: Minimal privacy guarantee
        ///
        /// DEFAULT: 1.0 (reasonable starting point; adjust based on sensitivity analysis)
        /// </summary>
        [DataMember(Name = "epsilon")]
        public double Epsilon { get; set; } = 1.0;

        /// <summary>
        /// Delta parameter for (epsilon, delta)-differential privacy.
        /// Represents the probability of privacy failure. Should be cryptographically small.
        ///
        /// GUIDANCE:
        /// - For (ε,δ)-differential privacy, δ should be much smaller than 1/n where n is dataset size
        /// - Typical values: 1e-5 to 1e-8 for healthcare datasets
        /// - δ = 0 gives pure ε-differential privacy (Laplace mechanism)
        /// - Only applies to Gaussian mechanism; Laplace mechanism has δ=0
        ///
        /// DEFAULT: 1e-5 (appropriate for datasets of up to ~100,000 records)
        /// </summary>
        [DataMember(Name = "delta")]
        public double Delta { get; set; } = 1e-5;

        /// <summary>
        /// Sensitivity of the query function (global sensitivity).
        /// Measures the maximum change in output when one record is added/removed.
        ///
        /// GUIDANCE:
        /// - For counting queries: sensitivity = 1
        /// - For sum queries: sensitivity = max possible value
        /// - For average queries: sensitivity = range / n
        /// - Higher sensitivity requires more noise for same epsilon
        ///
        /// DEFAULT: 1.0 (appropriate for counts and bounded numeric fields)
        /// </summary>
        [DataMember(Name = "sensitivity")]
        public double Sensitivity { get; set; } = 1.0;

        /// <summary>
        /// Maximum cumulative epsilon budget before warning.
        ///
        /// COMPOSITION: Under sequential composition, total privacy loss is sum of individual ε values.
        /// Advanced composition theorems can provide tighter bounds but are not yet implemented.
        ///
        /// DEFAULT: 1.0 (reasonable for most healthcare research applications per NIST guidance)
        ///
        /// WARNING: Exceeding this budget across multiple queries degrades privacy guarantees.
        /// </summary>
        [DataMember(Name = "maxCumulativeEpsilon")]
        public double MaxCumulativeEpsilon { get; set; } = 1.0;

        /// <summary>
        /// Whether to use advanced composition for better privacy accounting.
        ///
        /// ADVANCED COMPOSITION THEOREM (Dwork et al.):
        /// k queries with (ε,δ)-DP satisfy (ε', kδ+δ')-DP where:
        /// ε' ≈ √(2k ln(1/δ')) * ε + k*ε*(e^ε - 1)
        ///
        /// This can significantly improve privacy accounting for many queries.
        ///
        /// DEFAULT: false (uses simple sequential composition: total ε = Σε_i)
        ///
        /// NOTE: Advanced composition is not yet implemented. Setting this to true will
        /// log a warning and fall back to sequential composition.
        /// </summary>
        [DataMember(Name = "useAdvancedComposition")]
        public bool UseAdvancedComposition { get; set; } = false;

        /// <summary>
        /// Noise mechanism to use for differential privacy.
        ///
        /// MECHANISMS:
        /// - "laplace": Laplace mechanism (ε-DP, δ=0). Standard choice for numeric queries.
        ///   Noise scale = sensitivity/ε. Use for unbounded queries.
        /// - "gaussian": Gaussian mechanism ((ε,δ)-DP). Use when approximate DP is acceptable.
        ///   Requires δ > 0. Better utility for large datasets. Use for L2-sensitivity queries.
        /// - "exponential": Exponential mechanism. For categorical/selection queries.
        ///   Currently implemented using Laplace for numeric data.
        ///
        /// DEFAULT: "laplace" (provides pure ε-differential privacy)
        /// </summary>
        [DataMember(Name = "mechanism")]
        public string Mechanism { get; set; } = "laplace";

        /// <summary>
        /// When <see langword="true"/>, the engine tracks cumulative epsilon usage across
        /// all differential privacy operations and emits a warning when the total exceeds
        /// <see cref="MaxCumulativeEpsilon"/>. This helps operators stay within their
        /// overall privacy budget when multiple fields are independently perturbed.
        /// When <see langword="false"/> (default), no budget tracking is performed.
        /// </summary>
        [DataMember(Name = "privacyBudgetTrackingEnabled")]
        public bool PrivacyBudgetTrackingEnabled { get; set; } = false;

        /// <summary>
        /// When <see langword="true"/>, input values are clipped to a bounded range before
        /// noise is added. Clipping is required to bound the sensitivity of the query
        /// function, which is a prerequisite for the Gaussian mechanism to provide
        /// meaningful (ε,δ)-differential privacy guarantees. The clipping bounds are
        /// derived from the configured <see cref="Sensitivity"/>.
        /// When <see langword="false"/> (default), values are not clipped prior to noise
        /// injection.
        /// </summary>
        [DataMember(Name = "clippingEnabled")]
        public bool ClippingEnabled { get; set; } = false;
    }
}
