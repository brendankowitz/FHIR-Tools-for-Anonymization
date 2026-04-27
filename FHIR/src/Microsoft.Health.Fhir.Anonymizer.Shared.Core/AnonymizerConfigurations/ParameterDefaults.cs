using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    /// <summary>
    /// Central repository of default values and security constants for anonymization parameter
    /// configuration. Shared constants referenced by configuration validation and independently testable.
    /// </summary>
    public static class ParameterDefaults
    {
        /// <summary>
        /// Minimum allowed value for DateShiftFixedOffsetInDays (inclusive).
        /// Aligns with HIPAA Safe Harbor §164.514(b)(2)(i) date-shifting guidance.
        /// </summary>
        public const int MinDateShiftOffsetDays = -365;

        /// <summary>
        /// Maximum allowed value for DateShiftFixedOffsetInDays (inclusive).
        /// Aligns with HIPAA Safe Harbor §164.514(b)(2)(i) date-shifting guidance.
        /// </summary>
        public const int MaxDateShiftOffsetDays = 365;

        /// <summary>
        /// Minimum required length (in characters) for the cryptographic hash key.
        /// Keys shorter than this do not provide adequate entropy for HMAC-SHA256.
        /// Generate a secure key with: openssl rand -base64 32
        /// </summary>
        public const int MinCryptoHashKeyLength = 32;

        /// <summary>
        /// Placeholder patterns indicating an insecure or template cryptographic key.
        /// Keys matching any of these patterns must never be used in production.
        /// A SecurityException is thrown when any pattern is detected in a key value.
        /// <para>
        /// All patterns are stored in uppercase. The key validation method normalizes
        /// input via .ToUpperInvariant() before substring matching, making comparisons
        /// effectively case-insensitive. Any new entry added here must also be uppercase.
        /// </para>
        /// NOTE: Does NOT include anonymization output markers ("REDACTED", "ANONYMIZED");
        /// see <see cref="AnonymizationOutputMarkers"/>.
        /// </summary>
        internal static readonly ImmutableArray<string> DangerousPlaceholderPatterns =
            ImmutableArray.Create(
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
            );

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
        /// </summary>
        public static readonly ImmutableHashSet<int> ValidAesKeySizeBits =
            ImmutableHashSet.Create(128, 192, 256);

        static ParameterDefaults()
        {
            // Verify no output marker is accidentally listed as a dangerous placeholder.
            foreach (var marker in AnonymizationOutputMarkers)
            {
                Debug.Assert(
                    !DangerousPlaceholderPatterns.Contains(marker),
                    $"AnonymizationOutputMarkers entry '{marker}' must not appear in DangerousPlaceholderPatterns.");
            }

            // Verify all dangerous placeholder patterns are stored in uppercase,
            // enforcing the contract with the ToUpperInvariant() normalization in key validation.
            foreach (var pattern in DangerousPlaceholderPatterns)
            {
                Debug.Assert(
                    pattern == pattern.ToUpperInvariant(),
                    $"DangerousPlaceholderPatterns entry '{pattern}' is not uppercase.");
            }
        }
    }
}
