using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    /// <summary>
    /// Processor for k-anonymity generalization operations
    /// 
    /// IMPORTANT: This processor applies generalization strategies to individual nodes
    /// but does NOT guarantee k-anonymity without proper batch validation.
    /// 
    /// K-anonymity requires:
    /// 1. Identifying quasi-identifiers across ALL records in a dataset
    /// 2. Building equivalence classes (groups with identical quasi-identifier values)
    /// 3. Verifying each equivalence class has at least k members
    /// 4. Suppressing or further generalizing records in small equivalence classes
    /// 
    /// This processor only performs step 1 (generalization). Use KAnonymityValidator
    /// to verify the k-anonymity property holds after processing the full dataset.
    /// </summary>
    public class KAnonymityProcessor : IAnonymizerProcessor
    {
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<KAnonymityProcessor>();

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            EnsureArg.IsNotNull(node);
            EnsureArg.IsNotNull(settings);

            var result = new ProcessResult();

            // Skip if already visited
            if (context?.VisitedNodes?.Contains(node) == true)
            {
            return result;
            }

            var kAnonymitySetting = KAnonymitySetting.CreateFromRuleSettings(settings);

            // Log prominent warning: this does NOT guarantee k-anonymity
            _logger.LogWarning("╔═══════════════════════════════════════════════════════════════════╗");
            _logger.LogWarning("║ K-ANONYMITY WARNING: Generalization applied to single node only  ║");
            _logger.LogWarning("║ This does NOT guarantee k-anonymity without batch validation!    ║");
            _logger.LogWarning("║ Use KAnonymityValidator on the complete dataset after processing ║");
            _logger.LogWarning("╚═══════════════════════════════════════════════════════════════════╝");

            // Apply generalization to the current node based on strategy
            ApplyGeneralization(node, kAnonymitySetting, result);

            // Mark node and descendants as visited
            if (context?.VisitedNodes != null)
            {
                context.VisitedNodes.UnionWith(node.Descendants().CastElementNodes());
            }

            result.AddProcessRecord(AnonymizationOperations.KAnonymity, node);
            result.AddPrivacyMetric("k-value", kAnonymitySetting.KValue);
            result.AddPrivacyMetric("generalization-strategy", kAnonymitySetting.GeneralizationStrategy);
            result.AddPrivacyMetric("requires-batch-validation", true);

            return result;
        }

        private void ApplyGeneralization(ElementNode node, KAnonymitySetting setting, ProcessResult result)
        {
            if (node?.Value == null)
            {
                return;
            }

            switch (setting.GeneralizationStrategy.ToLowerInvariant())
            {
                case "range":
                    GeneralizeToRange(node);
                    result.AddProcessRecord(AnonymizationOperations.Abstract, node);
                    break;
                case "hierarchy":
                    GeneralizeByHierarchy(node);
                    result.AddProcessRecord(AnonymizationOperations.Abstract, node);
                    break;
                case "suppression":
                    SuppressValue(node, setting.SuppressionStrategy);
                    result.AddProcessRecord(AnonymizationOperations.Redact, node);
                    break;
                default:
                    _logger.LogWarning($"Unknown generalization strategy: {setting.GeneralizationStrategy}. Applying range generalization.");
                    GeneralizeToRange(node);
                    result.AddProcessRecord(AnonymizationOperations.Abstract, node);
                    break;
            }
        }

        /// <summary>
        /// Generalize numeric values to ranges
        /// </summary>
        private void GeneralizeToRange(ElementNode node)
        {
            if (node?.Value == null)
            {
                return;
            }

            var valueStr = node.Value.ToString();

            // Try to parse as integer for age-like values
            if (int.TryParse(valueStr, out int intValue))
            {
                // Generalize to age ranges
                if (intValue < 10)
                {
                    node.Value = "0-9";
                }
                else if (intValue < 20)
                {
                    node.Value = "10-19";
                }
                else if (intValue < 30)
                {
                    node.Value = "20-29";
                }
                else if (intValue < 40)
                {
                    node.Value = "30-39";
                }
                else if (intValue < 50)
                {
                    node.Value = "40-49";
                }
                else if (intValue < 60)
                {
                    node.Value = "50-59";
                }
                else if (intValue < 70)
                {
                    node.Value = "60-69";
                }
                else if (intValue < 80)
                {
                    node.Value = "70-79";
                }
                else if (intValue < 90)
                {
                    node.Value = "80-89";
                }
                else
                {
                    node.Value = "90+";
                }
                return;
            }

            // Try to parse as decimal
            if (decimal.TryParse(valueStr, out decimal decimalValue))
            {
                // Round to nearest 10 for general numeric values
                var rounded = Math.Round(decimalValue / 10) * 10;
                node.Value = rounded.ToString();
                return;
            }

            // For string values like postal codes, generalize by truncation
            if (valueStr.Length > 3)
            {
                node.Value = valueStr.Substring(0, 3) + "**";
            }
        }

        /// <summary>
        /// Generalize by hierarchical suppression (e.g., zip code: 12345 -> 123**)
        /// </summary>
        private void GeneralizeByHierarchy(ElementNode node)
        {
            if (node?.Value == null)
            {
                return;
            }

            var valueStr = node.Value.ToString();

            // For postal/zip codes - keep first 3 digits
            if (valueStr.Length >= 5 && valueStr.All(c => char.IsDigit(c) || c == '-'))
            {
                var digits = new string(valueStr.Where(char.IsDigit).ToArray());
                if (digits.Length >= 3)
                {
                    node.Value = digits.Substring(0, 3) + "**";
                    return;
                }
            }

            // For other strings, truncate to first character or first word
            if (valueStr.Contains(' '))
            {
                var firstWord = valueStr.Split(' ')[0];
                node.Value = firstWord;
            }
            else if (valueStr.Length > 1)
            {
                node.Value = valueStr.Substring(0, 1) + new string('*', Math.Min(valueStr.Length - 1, 3));
            }
        }

        /// <summary>
        /// Suppress value completely based on strategy
        /// </summary>
        private void SuppressValue(ElementNode node, string strategy)
        {
            if (node == null)
            {
                return;
            }

            switch (strategy.ToLowerInvariant())
            {
                case "redact":
                    node.Value = null;
                    break;
                case "remove":
                    // Set to null for removal
                    node.Value = null;
                    break;
                default:
                    node.Value = null;
                    break;
            }
        }
    }
}
