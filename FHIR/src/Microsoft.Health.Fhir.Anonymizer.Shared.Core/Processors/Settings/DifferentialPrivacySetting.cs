using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings
{
    /// <summary>
    /// Differential privacy mechanism types
    /// </summary>
    public enum DPMechanism
    {
        /// <summary>
        /// Laplace mechanism for numeric queries (pure epsilon-DP)
        /// </summary>
        Laplace,

        /// <summary>
        /// Gaussian mechanism for numeric queries (epsilon,delta-DP)
        /// </summary>
        Gaussian,

        /// <summary>
        /// Exponential mechanism for categorical/selection queries
        /// </summary>
        Exponential
    }

    /// <summary>
    /// Configuration settings for differential privacy processing
    /// </summary>
    public class DifferentialPrivacySetting
    {
        private static readonly ILogger s_logger = AnonymizerLogging.CreateLogger<DifferentialPrivacySetting>();

        /// <summary>
        /// Privacy budget parameter (smaller = more privacy, more noise)
        /// </summary>
        public double Epsilon { get; set; } = 0.1;

        /// <summary>
        /// Failure probability for (epsilon,delta)-DP (used with Gaussian mechanism)
        /// </summary>
        public double Delta { get; set; } = 1e-5;

        /// <summary>
        /// Sensitivity of the query (maximum change in output from changing one record)
        /// </summary>
        public double Sensitivity { get; set; } = 1.0;

        /// <summary>
        /// Differential privacy mechanism to use
        /// </summary>
        public DPMechanism Mechanism { get; set; } = DPMechanism.Laplace;

        /// <summary>
        /// Create DifferentialPrivacySetting from rule configuration dictionary
        /// </summary>
        public static DifferentialPrivacySetting CreateFromRuleSettings(Dictionary<string, object> settings)
        {
            var setting = new DifferentialPrivacySetting();

            if (settings.TryGetValue(RuleKeys.Epsilon, out var epsilonObj))
            {
                if (epsilonObj is double epsilonDbl)
                {
                    setting.Epsilon = epsilonDbl;
                }
                else if (double.TryParse(epsilonObj?.ToString(), out double epsilonParsed))
                {
                    setting.Epsilon = epsilonParsed;
                }
            }

            if (settings.TryGetValue(RuleKeys.Delta, out var deltaObj))
            {
                if (deltaObj is double deltaDbl)
                {
                    setting.Delta = deltaDbl;
                }
                else if (double.TryParse(deltaObj?.ToString(), out double deltaParsed))
                {
                    setting.Delta = deltaParsed;
                }
            }

            if (settings.TryGetValue(RuleKeys.Sensitivity, out var sensitivityObj))
            {
                if (sensitivityObj is double sensitivityDbl)
                {
                    setting.Sensitivity = sensitivityDbl;
                }
                else if (double.TryParse(sensitivityObj?.ToString(), out double sensitivityParsed))
                {
                    setting.Sensitivity = sensitivityParsed;
                }
            }

            if (settings.TryGetValue(RuleKeys.Mechanism, out var mechanismObj))
            {
                var mechanismStr = mechanismObj?.ToString();
                if (Enum.TryParse<DPMechanism>(mechanismStr, true, out var mechanism))
                {
                    setting.Mechanism = mechanism;
                }
            }

            // Validation with privacy-aware error messages
            ValidateEpsilon(setting.Epsilon);
            
            if (setting.Mechanism == DPMechanism.Gaussian && setting.Delta <= 0)
            {
                throw new ArgumentException("Delta must be greater than 0 for Gaussian mechanism");
            }

            if (setting.Sensitivity <= 0)
            {
                throw new ArgumentException("Sensitivity must be greater than 0");
            }

            return setting;
        }

        /// <summary>
        /// Validate epsilon value with privacy-aware warnings and hard limits
        /// NIST SP 800-188 guidance:
        /// - ε ≤ 0.1: Strong privacy (recommended for sensitive health data)
        /// - ε = 0.5-1.0: Moderate privacy
        /// - ε = 1.0-10.0: Weak privacy
        /// - ε > 10: Minimal privacy guarantee
        /// </summary>
        private static void ValidateEpsilon(double epsilon)
        {
            if (epsilon <= 0)
            {
                throw new ArgumentException(
                    "Epsilon must be greater than 0. " +
                    "Smaller epsilon values provide stronger privacy guarantees but add more noise. " +
                    "Recommended: ε ≤ 0.1 for sensitive health data (NIST SP 800-188).");
            }

            // SECURITY: Hard limit at epsilon = 10.0
            // Above this threshold, differential privacy provides negligible protection
            if (epsilon > 10.0)
            {
                throw new ArgumentException(
                    $"Epsilon value {epsilon} exceeds maximum allowed value of 10.0. " +
                    "PRIVACY RISK: Epsilon values above 10 provide minimal privacy protection. " +
                    "Such high values offer little advantage over non-private mechanisms and may " +
                    "create false confidence in privacy guarantees. " +
                    "\nRECOMMENDATIONS: " +
                    "\n  - For sensitive health data: ε ≤ 0.1 (strong privacy per NIST SP 800-188) " +
                    "\n  - For general use: ε ≤ 1.0 (moderate privacy) " +
                    "\n  - Maximum acceptable: ε ≤ 10.0 (weak privacy) " +
                    "\n\nIf you require ε > 10.0, consider whether differential privacy is the appropriate " +
                    "anonymization method for your use case.");
            }

            // Warning for values above 1.0 but below hard limit
            if (epsilon > 1.0)
            {
                s_logger.LogWarning(
                    "═══════════════════════════════════════════════════════════════════\n" +
                    "║ PRIVACY WARNING: High Epsilon Value Detected                    ║\n" +
                    "╠═════════════════════════════════════════════════════════════════╣\n" +
                    $"║ Epsilon: {epsilon,-54} ║\n" +
                    "║                                                                 ║\n" +
                    "║ This epsilon value provides WEAK privacy protection.           ║\n" +
                    "║                                                                 ║\n" +
                    "║ IMPLICATIONS:                                                   ║\n" +
                    "║ - High epsilon = less noise = weaker privacy guarantees         ║\n" +
                    "║ - Individual data points may be more identifiable               ║\n" +
                    "║ - Increased risk of privacy violations                          ║\n" +
                    "║                                                                 ║\n" +
                    "║ NIST SP 800-188 GUIDANCE FOR HEALTH DATA:                       ║\n" +
                    "║ - Strong privacy (recommended): ε ≤ 0.1                         ║\n" +
                    "║ - Moderate privacy: ε = 0.5-1.0                                 ║\n" +
                    "║ - Weak privacy (current): ε = 1.0-10.0                          ║\n" +
                    "║                                                                 ║\n" +
                    "║ RECOMMENDATIONS:                                                ║\n" +
                    "║ 1. Review regulatory requirements (HIPAA/GDPR/FDA)              ║\n" +
                    "║ 2. Consult with privacy officer or legal counsel                ║\n" +
                    "║ 3. Document justification for high epsilon in audit logs        ║\n" +
                    "║ 4. Consider if differential privacy is appropriate method       ║\n" +
                    "║ 5. Evaluate data utility vs. privacy tradeoffs                  ║\n" +
                    "╚═════════════════════════════════════════════════════════════════╝");
            }
        }
    }
}
