using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings
{
    /// <summary>
    /// Configuration settings for k-anonymity processing
    /// </summary>
    public class KAnonymitySetting
    {
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

            return setting;
        }
    }
}
