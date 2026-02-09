using System;
using System.Collections.Generic;

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

            // Validation
            if (setting.Epsilon <= 0)
            {
                throw new ArgumentException("Epsilon must be greater than 0");
            }

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
    }
}
