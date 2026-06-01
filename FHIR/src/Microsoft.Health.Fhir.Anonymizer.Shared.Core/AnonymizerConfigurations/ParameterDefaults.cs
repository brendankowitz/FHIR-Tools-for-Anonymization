using System.Collections.Generic;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    /// <summary>
    /// Default and boundary values for anonymization configuration parameters.
    /// Centralizes constants previously scattered across <see cref="ParameterConfiguration"/>
    /// and <see cref="ParameterConfigurationValidator"/>.
    /// </summary>
    public static class ParameterDefaults
    {
        /// <summary>
        /// Minimum allowed value for <see cref="ParameterConfiguration.DateShiftFixedOffsetInDays"/> (inclusive).
        /// </summary>
        public const int MinDateShiftOffsetDays = -365;

        /// <summary>
        /// Maximum allowed value for <see cref="ParameterConfiguration.DateShiftFixedOffsetInDays"/> (inclusive).
        /// </summary>
        public const int MaxDateShiftOffsetDays = 365;

        /// <summary>
        /// Minimum required length (in characters) for <see cref="ParameterConfiguration.CryptoHashKey"/>.
        /// Keys shorter than this value do not provide adequate entropy for HMAC-SHA256.
        /// </summary>
        public const int MinCryptoHashKeyLength = 32;

        /// <summary>
        /// Dangerous placeholder patterns that should never appear in production cryptographic keys.
        /// SECURITY: These patterns identify template/example values that provide no security.
        /// Case-insensitive comparison is used when checking keys against these patterns.
        /// </summary>
        public static readonly IReadOnlyList<string> DangerousPlaceholderPatterns = new[]
        {
            "TODO",
            "FIXME",
            "PLACEHOLDER",
            "YOUR_KEY",
            "YOUR-KEY",
            "REPLACE_ME",
            "REPLACE-ME",
            "INSERT_KEY",
            "INSERT-KEY",
            "SAMPLE",
            "EXAMPLE",
            "CHANGEME",
            "CHANGE_ME",
            "CHANGE-ME",
            "TEST_KEY",
            "TEST-KEY",
            "DUMMY",
            "FAKE",
        };
    }
}
