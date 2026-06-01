using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
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
        /// <remarks>
        /// Retained for backward compatibility. New code should use
        /// <see cref="ParameterDefaults.MinDateShiftOffsetDays"/>.
        /// </remarks>
        [Obsolete("Use ParameterDefaults.MinDateShiftOffsetDays")]
        public const int MinDateShiftOffsetDays = ParameterDefaults.MinDateShiftOffsetDays;

        /// <summary>
        /// Maximum allowed value for <see cref="DateShiftFixedOffsetInDays"/> (inclusive).
        /// </summary>
        /// <remarks>
        /// Retained for backward compatibility. New code should use
        /// <see cref="ParameterDefaults.MaxDateShiftOffsetDays"/>.
        /// </remarks>
        [Obsolete("Use ParameterDefaults.MaxDateShiftOffsetDays")]
        public const int MaxDateShiftOffsetDays = ParameterDefaults.MaxDateShiftOffsetDays;

        /// <summary>
        /// Minimum required length (in characters) for <see cref="CryptoHashKey"/>.
        /// Keys shorter than this value do not provide adequate entropy for HMAC-SHA256.
        /// </summary>
        /// <remarks>
        /// Retained for backward compatibility. New code should use
        /// <see cref="ParameterDefaults.MinCryptoHashKeyLength"/>.
        /// </remarks>
        [Obsolete("Use ParameterDefaults.MinCryptoHashKeyLength")]
        public const int MinCryptoHashKeyLength = ParameterDefaults.MinCryptoHashKeyLength;

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
        ///   <item><description><c>Resource</c> - each resource receives its own deterministic offset derived from its ID and <see cref="DateShiftKey"/>.</description></item>
        ///   <item><description><c>File</c> - all resources in the same input file share a single offset.</description></item>
        ///   <item><description><c>Folder</c> - all resources in the same folder share a single offset.</description></item>
        /// </list>
        /// Narrower scopes (Resource) maximise per-record randomness; wider scopes (Folder)
        /// preserve temporal relationships across records processed together.
        /// </summary>
        [DataMember(Name = "dateShiftScope")]
        public DateShiftScope DateShiftScope { get; set; }

        /// <summary>
        /// Optional fixed date-shift offset in days. When set, overrides the deterministic
        /// key-based date shift. Must be in the range
        /// [<see cref="ParameterDefaults.MinDateShiftOffsetDays"/>,
        ///  <see cref="ParameterDefaults.MaxDateShiftOffsetDays"/>] (i.e. -365 to +365).
        /// When null the cryptographic key-based shift is used instead.
        /// </summary>
        [DataMember(Name = "dateShiftFixedOffsetInDays")]
        public int? DateShiftFixedOffsetInDays { get; set; }

        /// <summary>
        /// Key used for HMAC-SHA256 cryptographic hashing of identifiers.
        /// Must be >= <see cref="ParameterDefaults.MinCryptoHashKeyLength"/> characters
        /// (non-whitespace) to ensure adequate entropy. Whitespace-only values are rejected.
        /// Generate a secure key using: openssl rand -base64 32
        /// </summary>
        [DataMember(Name = "cryptoHashKey")]
        public string CryptoHashKey { get; set; }

        /// <summary>
        /// AES symmetric encryption key used by the encrypt anonymization method.
        /// The key must encode to exactly 16, 24, or 32 UTF-8 bytes, corresponding to
        /// AES-128, AES-192, and AES-256 respectively. Keys of any other length are
        /// rejected during <see cref="Validate"/>. Generate a 256-bit key with:
        ///   openssl rand -base64 32
        /// </summary>
        [DataMember(Name = "encryptKey")]
        public string EncryptKey { get; set; }

        /// <summary>
        /// When true, ages 90 and above are fully redacted while ages below 90 are retained
        /// as-is, following the HIPAA Safe Harbor de-identification standard which treats
        /// ages >= 90 as a direct identifier.
        /// When false (default), all age values are redacted.
        /// </summary>
        [DataMember(Name = "enablePartialAgesForRedact")]
        public bool EnablePartialAgesForRedact { get; set; }

        /// <summary>
        /// When true, only the year component of a date value is retained during redaction;
        /// month and day are removed. This preserves limited temporal utility while reducing
        /// re-identification risk.
        /// When false (default), date values are fully redacted.
        /// </summary>
        [DataMember(Name = "enablePartialDatesForRedact")]
        public bool EnablePartialDatesForRedact { get; set; }

        /// <summary>
        /// When true, the first three digits of a ZIP code are retained during redaction,
        /// unless the prefix appears in <see cref="RestrictedZipCodeTabulationAreas"/>,
        /// in which case the entire ZIP code is redacted. This aligns with HIPAA Safe Harbor,
        /// which permits the 3-digit prefix for geographic areas with a population >= 20,000.
        /// When false (default), ZIP codes are fully redacted.
        /// </summary>
        [DataMember(Name = "enablePartialZipCodesForRedact")]
        public bool EnablePartialZipCodesForRedact { get; set; }

        /// <summary>
        /// List of 3-digit ZIP code prefixes (ZIP Code Tabulation Areas) that must be
        /// fully redacted because the corresponding geographic area has fewer than 20,000
        /// people, per HIPAA Safe Harbor section 164.514(b)(2)(i).
        /// Only evaluated when <see cref="EnablePartialZipCodesForRedact"/> is true.
        /// </summary>
        [DataMember(Name = "restrictedZipCodeTabulationAreas")]
        public List<string> RestrictedZipCodeTabulationAreas { get; set; }

        /// <summary>
        /// Optional configuration for k-anonymity post-processing.
        /// When null (default), k-anonymity post-processing is disabled.
        /// </summary>
        [DataMember(Name = "kAnonymitySettings")]
        public KAnonymityParameterConfiguration KAnonymitySettings { get; set; }

        /// <summary>
        /// Optional configuration for differential privacy noise injection.
        /// When null (default), differential privacy is disabled.
        /// </summary>
        [DataMember(Name = "differentialPrivacySettings")]
        public DifferentialPrivacyParameterConfiguration DifferentialPrivacySettings { get; set; }

        /// <summary>
        /// Extension point for tool-specific or experimental settings, stored as an
        /// arbitrary JSON object. The anonymizer engine does not interpret this field;
        /// it is passed through as-is to custom processors that may inspect it.
        /// </summary>
        [DataMember(Name = "customSettings")]
        public JObject CustomSettings { get; set; }

        /// <summary>
        /// Optional prefix prepended to the resource (or file/folder) identifier before
        /// HMAC computation during date shifting. Useful for namespace isolation when the
        /// same <see cref="DateShiftKey"/> is reused across multiple datasets.
        /// </summary>
        public string DateShiftKeyPrefix { get; set; }

        /// <summary>
        /// Validate configuration for security issues and placeholder values.
        /// Delegates to <see cref="ParameterConfigurationValidator.Validate"/> which contains
        /// the full validation logic.
        ///
        /// SECURITY: Rejects dangerous placeholder values that should never be used in production.
        /// This prevents accidental use of example/template configurations with insecure dummy keys.
        /// Throws SecurityException for placeholder keys to ensure fail-secure behavior.
        /// </summary>
        public void Validate()
        {
            ParameterConfigurationValidator.Validate(this);
        }
    }

    /// <summary>
    /// Configuration parameters for k-anonymity processing.
    /// </summary>
    [DataContract]
    public class KAnonymityParameterConfiguration
    {
        /// <summary>
        /// Minimum group size for k-anonymity (default: 5).
        /// Each combination of quasi-identifiers must appear in at least k records.
        /// Higher values provide stronger privacy but may require more aggressive generalization.
        /// </summary>
        [DataMember(Name = "kValue")]
        public int KValue { get; set; } = 5;

        /// <summary>
        /// List of FHIR paths to quasi-identifiers.
        /// Example: ["Patient.birthDate", "Patient.address.postalCode", "Patient.gender"]
        /// </summary>
        [DataMember(Name = "quasiIdentifiers")]
        public List<string> QuasiIdentifiers { get; set; }

        /// <summary>
        /// Generalization hierarchies for quasi-identifiers (optional).
        /// Maps FHIR path to generalization strategy configuration.
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
    /// - Dwork, C., and Roth, A. (2014). "The Algorithmic Foundations of Differential Privacy."
    /// </summary>
    [DataContract]
    public class DifferentialPrivacyParameterConfiguration
    {
        /// <summary>
        /// Privacy budget (epsilon) - lower values provide stronger privacy.
        /// DEFAULT: 1.0 (reasonable starting point; adjust based on sensitivity analysis)
        /// </summary>
        [DataMember(Name = "epsilon")]
        public double Epsilon { get; set; } = 1.0;

        /// <summary>
        /// Delta parameter for (epsilon, delta)-differential privacy.
        /// Represents the probability of privacy failure. Should be cryptographically small.
        /// DEFAULT: 1e-5 (appropriate for datasets of up to ~100,000 records; scale down
        /// e.g. 1e-6 for larger datasets per NIST SP 800-226 guidance)
        /// </summary>
        [DataMember(Name = "delta")]
        public double Delta { get; set; } = 1e-5;

        /// <summary>
        /// Sensitivity of the query - maximum change in output for one record's addition/removal.
        /// Must be set based on the specific query being protected.
        /// DEFAULT: 1.0 (appropriate for counting queries)
        /// </summary>
        [DataMember(Name = "sensitivity")]
        public double Sensitivity { get; set; } = 1.0;

        /// <summary>
        /// Maximum cumulative privacy budget across all queries.
        /// Prevents epsilon budget exhaustion through repeated queries.
        /// DEFAULT: 1.0
        /// </summary>
        [DataMember(Name = "maxCumulativeEpsilon")]
        public double MaxCumulativeEpsilon { get; set; } = 1.0;

        /// <summary>
        /// When true, uses advanced composition theorems (e.g., the moments accountant)
        /// to allow tighter privacy budget accounting across multiple queries, yielding
        /// a lower effective epsilon for the same number of queries than basic composition.
        /// DEFAULT: false (basic composition provides conservative, simpler guarantees)
        /// </summary>
        [DataMember(Name = "useAdvancedComposition")]
        public bool UseAdvancedComposition { get; set; } = false;

        /// <summary>
        /// The differential privacy noise mechanism to use.
        /// Supported values: "Laplace" (default, for epsilon-DP), "Gaussian" (for (epsilon,delta)-DP).
        /// The Laplace mechanism adds noise proportional to sensitivity/epsilon.
        /// The Gaussian mechanism adds Gaussian noise calibrated to (epsilon, delta)-DP.
        /// JSON deserialization uses "mechanism" for backward compatibility with existing
        /// configuration files; the C# property name NoiseMechanism is used in code.
        /// </summary>
        [DataMember(Name = "mechanism")]
        public string NoiseMechanism { get; set; } = "Laplace";

        /// <summary>
        /// When true, tracks cumulative epsilon budget consumption across all queries in a session.
        /// Once the cumulative budget exceeds <see cref="MaxCumulativeEpsilon"/>, further queries
        /// are rejected to prevent budget exhaustion attacks.
        /// DEFAULT: false
        /// </summary>
        [DataMember(Name = "privacyBudgetTrackingEnabled")]
        public bool PrivacyBudgetTrackingEnabled { get; set; } = false;

        /// <summary>
        /// When true, enables sensitivity clipping to bound each individual record's contribution
        /// to the query result before noise is added. Clipping ensures no single record can
        /// inflate the sensitivity beyond the configured value, strengthening privacy guarantees.
        /// DEFAULT: false
        /// </summary>
        [DataMember(Name = "clippingEnabled")]
        public bool ClippingEnabled { get; set; } = false;
    }
}
