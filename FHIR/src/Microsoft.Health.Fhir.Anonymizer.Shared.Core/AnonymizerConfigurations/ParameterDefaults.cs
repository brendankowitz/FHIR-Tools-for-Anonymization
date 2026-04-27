using System.Collections.Generic;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    /// <summary>
    /// Central repository of default values and security constants for anonymization parameter
    /// configuration. Shared constants referenced by configuration validation and independently testable.
    /// </summary>
    public static class ParameterDefaults
    {
        /// <summary>
        /// Placeholder patterns indicating an insecure or template cryptographic key.
        /// Keys matching any of these patterns must never be used in production.
        /// A SecurityException is thrown when any pattern is detected in a key value.
        /// NOTE: Does NOT include anonymization output markers ("REDACTED", "ANONYMIZED");
        /// see <see cref="AnonymizationOutputMarkers"/>.
        /// </summary>
        public static readonly IReadOnlyList<string> DangerousPlaceholderPatterns = new[]
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
        /// Strings used as anonymization output markers in redacted or anonymized FHIR fields.
        /// These are legitimate output values and must NOT be confused with dangerous key placeholders.
        /// Kept separate from <see cref="DangerousPlaceholderPatterns"/>.
        /// </summary>
        public static readonly IReadOnlyList<string> AnonymizationOutputMarkers = new[]
        {
            "REDACTED",
            "[REDACTED]",
            "***",
            "ANONYMIZED"
        };

        /// <summary>
        /// Valid AES key sizes in bits: 128 (16 bytes), 192 (24 bytes), 256 (32 bytes).
        /// Used during validation to avoid allocating an Aes instance.
        /// NOTE: IReadOnlySet wrapper does not prevent mutation via casting.
        /// For .NET 8+, consider FrozenSet for true immutability.
        /// </summary>
        public static readonly IReadOnlySet<int> ValidAesKeySizeBits =
            new HashSet<int> { 128, 192, 256 };
    }
}
