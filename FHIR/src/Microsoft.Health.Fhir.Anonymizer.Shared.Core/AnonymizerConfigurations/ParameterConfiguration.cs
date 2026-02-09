using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    [DataContract]
    public class ParameterConfiguration
    {
        private static readonly ILogger s_logger = AnonymizerLogging.CreateLogger<ParameterConfiguration>();

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

        [DataMember(Name = "dateShiftFixedOffsetInDays")]
        public int? DateShiftFixedOffsetInDays { get; set; }

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
        /// Validate a key parameter doesn't contain placeholder values.
        /// SECURITY CRITICAL: Prevents use of example/template keys in production.
        /// </summary>
        private void ValidateKeyParameter(string keyValue, string parameterName, string keyType)
        {
            if (string.IsNullOrEmpty(keyValue))
            {
                return; // Empty keys are allowed if the feature is not used
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
                    "This provides weaker privacy guarantees. Consider using epsilon ≤ 1.0 for moderate privacy " +
                    "or epsilon ≤ 0.1 for strong privacy (NIST SP 800-188 guidance for health data).");
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
                    "K-anonymity k-value is 2 (minimal). Consider k ≥ 5 for better privacy protection " +
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
        public Dictionary<string, JObject> GeneralizationHierarchies { get; set; }

        /// <summary>
        /// Suppression threshold: if more than this percentage of records need suppression, emit warning.
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
        /// DEFAULT: 0.1 (strong privacy)
        /// 
        /// WARNING: Epsilon values accumulate across queries via sequential composition.
        /// Total privacy loss = sum of all epsilon values consumed.
        /// </summary>
        [DataMember(Name = "epsilon")]
        public double Epsilon { get; set; } = 0.1;

        /// <summary>
        /// Failure probability (delta) - probability that privacy guarantee fails.
        /// 
        /// GUIDANCE:
        /// - For (ε,δ)-differential privacy, δ should be much smaller than 1/n where n is dataset size
        /// - Typical values: 1e-5 to 1e-10
        /// - Smaller delta = stronger privacy guarantee
        /// - Only applies to Gaussian mechanism; Laplace mechanism has δ=0
        /// 
        /// DEFAULT: 1e-5
        /// </summary>
        [DataMember(Name = "delta")]
        public double Delta { get; set; } = 1e-5;

        /// <summary>
        /// Global sensitivity for the query/transformation.
        /// Maximum change in output from adding/removing one record.
        /// 
        /// GUIDANCE:
        /// - For counting queries: sensitivity = 1
        /// - For sum queries on bounded values [min, max]: sensitivity = max - min
        /// - For mean/average: typically 1-10 depending on data range
        /// - Higher sensitivity requires more noise for same epsilon
        /// 
        /// DEFAULT: 1.0 (appropriate for counts and bounded numeric fields)
        /// </summary>
        [DataMember(Name = "sensitivity")]
        public double Sensitivity { get; set; } = 1.0;

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
    }
}
