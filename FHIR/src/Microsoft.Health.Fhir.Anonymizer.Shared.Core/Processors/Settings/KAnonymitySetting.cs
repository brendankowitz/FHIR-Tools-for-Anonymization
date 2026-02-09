using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings
{
    /// <summary>
    /// Configuration settings for k-anonymity processing
    /// </summary>
    public class KAnonymitySetting
    {
        private static readonly ILogger s_logger = AnonymizerLogging.CreateLogger<KAnonymitySetting>();

        /// <summary>
        /// Minimum group size for k-anonymity (k value)
        /// </summary>
        public int K { get; set; } = 5;

        /// <summary>
        /// FHIR paths to quasi-identifiers (e.g., "Patient.birthDate", "Patient.address.postalCode")
        /// </summary>
        public List<string> QuasiIdentifiers { get; set; } = new List<string>();

        /// <summary>
        /// Generalization hierarchy for quasi-identifiers
        /// Key: FHIR path, Value: generalization levels
        /// </summary>
        public Dictionary<string, List<string>> GeneralizationHierarchy { get; set; } = new Dictionary<string, List<string>>();

        /// <summary>
        /// Generalization strategy: "range", "hierarchy", or "suppression"
        /// </summary>
        public string GeneralizationStrategy { get; set; } = "range";

        /// <summary>
        /// Suppression strategy: "redact" or "remove"
        /// </summary>
        public string SuppressionStrategy { get; set; } = "redact";

        /// <summary>
        /// Alternate property name for k value (for backward compatibility)
        /// </summary>
        public int KValue
        {
            get => K;
            set => K = value;
        }

        /// <summary>
        /// Create KAnonymitySetting from rule configuration dictionary
        /// </summary>
        public static KAnonymitySetting CreateFromRuleSettings(Dictionary<string, object> settings)
        {
            var setting = new KAnonymitySetting();

            if (settings.TryGetValue(RuleKeys.KValue, out var kObj))
            {
                if (kObj is int kInt)
                {
                    setting.K = kInt;
                }
                else if (kObj is long kLong)
                {
                    setting.K = (int)kLong;
                }
                else if (int.TryParse(kObj?.ToString(), out int kParsed))
                {
                    setting.K = kParsed;
                }
            }

            if (settings.TryGetValue(RuleKeys.QuasiIdentifiers, out var qiObj))
            {
                if (qiObj is JArray jArray)
                {
                    setting.QuasiIdentifiers = jArray.Select(t => t.ToString()).ToList();
                }
                else if (qiObj is List<string> strList)
                {
                    setting.QuasiIdentifiers = strList;
                }
                else if (qiObj is string qiStr)
                {
                    setting.QuasiIdentifiers = qiStr.Split(',').Select(s => s.Trim()).ToList();
                }
            }

            if (settings.TryGetValue(RuleKeys.GeneralizationStrategy, out var strategyObj))
            {
                setting.GeneralizationStrategy = strategyObj?.ToString() ?? "range";
            }

            if (settings.TryGetValue(RuleKeys.SuppressionStrategy, out var suppressionObj))
            {
                setting.SuppressionStrategy = suppressionObj?.ToString() ?? "redact";
            }

            if (settings.TryGetValue(RuleKeys.GeneralizationHierarchy, out var hierarchyObj))
            {
                if (hierarchyObj is JObject jObj)
                {
                    setting.GeneralizationHierarchy = jObj.Properties()
                        .ToDictionary(
                            p => p.Name,
                            p => p.Value is JArray arr ? arr.Select(t => t.ToString()).ToList() : new List<string>()
                        );
                }
                else if (hierarchyObj is Dictionary<string, List<string>> dict)
                {
                    setting.GeneralizationHierarchy = dict;
                }
            }

            // Validate k-value during initialization
            ValidateKValue(setting.K);

            return setting;
        }

        /// <summary>
        /// Validate k-value with clear privacy-aware error messages
        /// K-anonymity requires k >= 2 to provide any privacy protection
        /// </summary>
        private static void ValidateKValue(int k)
        {
            if (k < 2)
            {
                throw new ArgumentException(
                    $"K-anonymity k-value must be at least 2 (provided: {k}). " +
                    "PRIVACY REQUIREMENT: k=1 provides NO privacy protection as each record forms its own equivalence class. " +
                    "\n\nK-ANONYMITY GUIDANCE: " +
                    "\n  - k=2: Minimal privacy (each record indistinguishable from at least 1 other) " +
                    "\n  - k=5: Recommended minimum for most use cases (HIPAA Safe Harbor guidance) " +
                    "\n  - k=10+: Strong privacy for sensitive data " +
                    "\n  - k=100+: Very strong privacy, but may require aggressive generalization " +
                    "\n\nHigher k values provide stronger privacy but may reduce data utility through " +
                    "increased generalization or suppression. Choose k based on your privacy requirements " +
                    "and regulatory obligations.");
            }

            if (k == 2)
            {
                s_logger.LogWarning(
                    "═══════════════════════════════════════════════════════════════════\n" +
                    "║ PRIVACY WARNING: Minimal K-Anonymity Value                      ║\n" +
                    "╠═════════════════════════════════════════════════════════════════╣\n" +
                    "║ k-value: 2 (minimum acceptable)                                ║\n" +
                    "║                                                                 ║\n" +
                    "║ This provides MINIMAL privacy protection.                       ║\n" +
                    "║                                                                 ║\n" +
                    "║ IMPLICATIONS:                                                   ║\n" +
                    "║ - Each record only needs 1 similar record for protection        ║\n" +
                    "║ - Vulnerable to background knowledge attacks                    ║\n" +
                    "║ - May not meet regulatory requirements                          ║\n" +
                    "║                                                                 ║\n" +
                    "║ RECOMMENDATIONS:                                                ║\n" +
                    "║ - HIPAA Safe Harbor: k ≥ 5 recommended                          ║\n" +
                    "║ - Sensitive health data: k ≥ 10 recommended                     ║\n" +
                    "║ - Consider increasing k for better privacy protection           ║\n" +
                    "║ - Document justification for low k-value                        ║\n" +
                    "╚═════════════════════════════════════════════════════════════════╝");
            }
        }
    }
}
