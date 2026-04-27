// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    /// <summary>
    /// Central repository for all hard-coded default values and boundary constants used
    /// across anonymization parameter configurations. Centralising these values makes
    /// compliance auditing straightforward: every default is declared exactly once and
    /// can be verified against regulatory requirements in a single file.
    /// </summary>
    public static class ParameterDefaults
    {
        #region DateShift

        /// <summary>Minimum allowed date-shift offset, in days.</summary>
        public const int MinDateShiftOffsetDays = -365;

        /// <summary>Maximum allowed date-shift offset, in days.</summary>
        public const int MaxDateShiftOffsetDays = 365;

        #endregion

        #region CryptoHash

        /// <summary>Minimum accepted HMAC key length, in bytes.</summary>
        public const int MinCryptoHashKeyLength = 32;

        #endregion

        #region Encrypt

        /// <summary>Set of AES key sizes (in bits) that are accepted by the encrypt processor.</summary>
        public static readonly IReadOnlySet<int> ValidAesKeySizeBits =
            new HashSet<int> { 128, 192, 256 };

        #endregion

        #region Security

        /// <summary>
        /// Patterns that, if used as substitution or placeholder values, would trivially
        /// reveal that data has been anonymized and could therefore be considered
        /// dangerous from a de-identification standpoint.
        /// </summary>
        public static readonly IReadOnlyList<string> DangerousPlaceholderPatterns =
            new[]
            {
                "REDACTED",
                "[REDACTED]",
                "***",
                "ANONYMIZED",
                "[ANONYMIZED]",
                "REMOVED",
                "[REMOVED]",
            };

        #endregion

        #region Redact

        /// <summary>Whether partial ages are preserved during redaction by default.</summary>
        public const bool EnablePartialAgesForRedact = false;

        /// <summary>Whether partial dates are preserved during redaction by default.</summary>
        public const bool EnablePartialDatesForRedact = false;

        /// <summary>Whether partial ZIP codes are preserved during redaction by default.</summary>
        public const bool EnablePartialZipCodesForRedact = false;

        #endregion

        #region KAnonymity

        /// <summary>Default k-value for k-anonymity (minimum group size).</summary>
        public const int KValue = 5;

        /// <summary>Default suppression threshold: fraction of records that may be suppressed to satisfy k-anonymity.</summary>
        public const double SuppressionThreshold = 0.3;

        #endregion

        #region DifferentialPrivacy

        /// <summary>Default privacy-loss budget (epsilon) per query.</summary>
        public const double Epsilon = 1.0;

        /// <summary>Default failure probability (delta) for (epsilon, delta)-DP.</summary>
        public const double Delta = 1e-5;

        /// <summary>Default sensitivity of the query function.</summary>
        public const double Sensitivity = 1.0;

        /// <summary>Default maximum cumulative epsilon across all queries in a session.</summary>
        public const double MaxCumulativeEpsilon = 1.0;

        /// <summary>Whether advanced (optimal) composition is used by default.</summary>
        public const bool UseAdvancedComposition = false;

        /// <summary>Default noise-injection mechanism (Laplace mechanism).</summary>
        public const string Mechanism = "laplace";

        /// <summary>Whether privacy budget tracking is enabled by default.</summary>
        public const bool PrivacyBudgetTrackingEnabled = false;

        /// <summary>Whether input clipping is applied by default before noise injection.</summary>
        public const bool ClippingEnabled = false;

        #endregion
    }
}
