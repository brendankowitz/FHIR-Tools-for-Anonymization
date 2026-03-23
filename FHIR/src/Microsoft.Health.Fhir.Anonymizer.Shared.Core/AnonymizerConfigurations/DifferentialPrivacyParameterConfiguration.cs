using System;
using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
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
        private static readonly ILogger s_logger = AnonymizerLogging.CreateLogger<DifferentialPrivacyParameterConfiguration>();

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
        /// all differential privacy operations and emits a warning when the total epsilon
        /// spend across operations exceeds <see cref="MaxCumulativeEpsilon"/>. This helps
        /// operators stay within their overall privacy budget when multiple fields are
        /// independently perturbed.
        /// When <see langword="false"/> (default), no budget tracking is performed.
        /// </summary>
        [DataMember(Name = "privacyBudgetTrackingEnabled")]
        public bool PrivacyBudgetTrackingEnabled { get; set; } = false;

        /// <summary>
        /// When <see langword="true"/>, input values are clipped to a bounded range before
        /// noise is added. Clipping bounds the sensitivity of the query function, which is
        /// a prerequisite for the Gaussian mechanism to provide meaningful
        /// (ε,δ)-differential privacy guarantees. Without clipping, the Gaussian mechanism
        /// may not satisfy its theoretical privacy bounds because unbounded inputs produce
        /// unbounded sensitivity. The clipping range is derived from the configured
        /// <see cref="Sensitivity"/>.
        /// When <see langword="false"/> (default), values are not clipped prior to noise
        /// injection.
        /// </summary>
        [DataMember(Name = "clippingEnabled")]
        public bool ClippingEnabled { get; set; } = false;

        /// <summary>
        /// Validates the differential privacy configuration parameters.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
        public void Validate()
        {
            if (Epsilon <= 0)
            {
                throw new ArgumentException("Differential privacy epsilon must be greater than 0");
            }

            if (Epsilon > 10.0)
            {
                throw new ArgumentException(
                    $"Differential privacy epsilon value {Epsilon} exceeds maximum of 10.0. " +
                    "High epsilon values provide minimal privacy protection. See configuration comments for guidance.");
            }

            if (Epsilon > 1.0)
            {
                s_logger.LogWarning(
                    $"Differential privacy epsilon value {Epsilon} is high (>1.0). " +
                    "This provides weaker privacy guarantees. Consider using epsilon <= 1.0 for moderate privacy " +
                    "or epsilon <= 0.1 for strong privacy (NIST SP 800-188 guidance for health data).");
            }

            if (Delta < 0 || Delta > 1)
            {
                throw new ArgumentException("Differential privacy delta must be between 0 and 1");
            }

            if (Sensitivity <= 0)
            {
                throw new ArgumentException("Differential privacy sensitivity must be greater than 0");
            }

            if (MaxCumulativeEpsilon <= 0)
            {
                throw new ArgumentException("Differential privacy maxCumulativeEpsilon must be greater than 0");
            }
        }
    }
}
