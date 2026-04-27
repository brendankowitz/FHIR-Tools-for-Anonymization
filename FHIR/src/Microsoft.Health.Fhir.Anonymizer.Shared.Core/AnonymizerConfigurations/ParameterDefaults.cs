using System;
using System.Collections.Immutable;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    /// <summary>
    /// Central repository for default values and constraints used across anonymization
    /// parameter configuration. Provides compile-time constants and immutable collections
    /// that guard against accidental mutation at runtime.
    /// </summary>
    public static class ParameterDefaults
    {
        /// <summary>
        /// Minimum allowed value for DateShiftFixedOffsetInDays (inclusive).
        /// </summary>
        public const int MinDateShiftOffsetDays = -365;

        /// <summary>
        /// Maximum allowed value for DateShiftFixedOffsetInDays (inclusive).
        /// </summary>
        public const int MaxDateShiftOffsetDays = 365;

        /// <summary>
        /// Minimum required length (in characters) for CryptoHashKey.
        /// Keys shorter than this value do not provide adequate entropy for HMAC-SHA256.
        /// </summary>
        public const int MinCryptoHashKeyLength = 32;

        /// <summary>
        /// Dangerous placeholder patterns that must be rejected.
        /// These are strings that commonly appear in example/template configurations
        /// and must never be used in production anonymization operations.
        /// Made public so external code can surface the same rejection logic without
        /// duplicating the pattern list.
        /// Using ImmutableArray prevents runtime mutation via casting to a mutable interface.
        /// </summary>
        public static readonly ImmutableArray<string> DangerousPlaceholderPatterns = ImmutableArray.Create(
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
        /// Strings that may appear in anonymized output fields.
        /// Can be used by downstream tools to detect whether a field has already been processed.
        /// Using ImmutableArray prevents runtime mutation via casting to a mutable interface
        /// such as IList or IReadOnlyList.
        /// </summary>
        public static readonly ImmutableArray<string> AnonymizationOutputMarkers = ImmutableArray.Create(
            "REDACTED",
            "[REDACTED]",
            "***",
            "ANONYMIZED"
        );

        /// <summary>
        /// Static constructor validates internal consistency of the constants above.
        /// Runs once at class initialization; any violation throws InvalidOperationException
        /// in both Debug and Release builds, providing fail-secure behavior if the constants
        /// are ever inadvertently changed.
        /// </summary>
        static ParameterDefaults()
        {
            if (MinDateShiftOffsetDays >= 0)
            {
                throw new InvalidOperationException(
                    $"ParameterDefaults invariant violation: MinDateShiftOffsetDays must be negative, " +
                    $"but was {MinDateShiftOffsetDays}.");
            }

            if (MaxDateShiftOffsetDays <= 0)
            {
                throw new InvalidOperationException(
                    $"ParameterDefaults invariant violation: MaxDateShiftOffsetDays must be positive, " +
                    $"but was {MaxDateShiftOffsetDays}.");
            }

            if (MinCryptoHashKeyLength <= 0)
            {
                throw new InvalidOperationException(
                    $"ParameterDefaults invariant violation: MinCryptoHashKeyLength must be positive, " +
                    $"but was {MinCryptoHashKeyLength}.");
            }

            if (MinDateShiftOffsetDays > MaxDateShiftOffsetDays)
            {
                throw new InvalidOperationException(
                    $"ParameterDefaults invariant violation: MinDateShiftOffsetDays ({MinDateShiftOffsetDays}) " +
                    $"must be less than or equal to MaxDateShiftOffsetDays ({MaxDateShiftOffsetDays}).");
            }
        }
    }
}
