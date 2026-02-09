using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    /// <summary>
    /// Processor for differential privacy anonymization using Laplace or Gaussian mechanisms
    /// </summary>
    public class DifferentialPrivacyProcessor : IAnonymizerProcessor
    {
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<DifferentialPrivacyProcessor>();
        private static readonly HashSet<string> s_numericTypeNames = new HashSet<string>
        {
            FHIRAllTypes.Decimal.ToString(),
            FHIRAllTypes.Integer.ToString(),
            FHIRAllTypes.PositiveInt.ToString(),
            FHIRAllTypes.UnsignedInt.ToString()
        };

        private static readonly HashSet<string> s_integerTypeNames = new HashSet<string>
        {
            FHIRAllTypes.Integer.ToString(),
            FHIRAllTypes.PositiveInt.ToString(),
            FHIRAllTypes.UnsignedInt.ToString()
        };

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            var result = new ProcessResult();

            // Handle null node or null settings gracefully
            if (node == null || settings == null)
            {
                return result;
            }

            // Skip if already visited
            if (context?.VisitedNodes?.Contains(node) == true)
            {
                return result;
            }

            var dpSetting = DifferentialPrivacySetting.CreateFromRuleSettings(settings);

            // SECURITY FIX: Require explicit budget context to prevent budget sharing across unrelated datasets
            var budgetContext = settings.GetValueOrDefault("budgetContext")?.ToString();
            if (string.IsNullOrEmpty(budgetContext))
            {
                _logger.LogError("Budget context not specified for differential privacy operation. Each dataset/file must have explicit budget context.");
                throw new InvalidOperationException("Budget context is required for differential privacy. Specify 'budgetContext' in settings to prevent unintended budget sharing across datasets.");
            }

            var tracker = PrivacyBudgetTracker.Instance;

            if (!tracker.ConsumeBudget(budgetContext, dpSetting.Epsilon))
            {
                _logger.LogError($"Privacy budget exceeded for context '{budgetContext}'. Operation aborted.");
                throw new InvalidOperationException($"Privacy budget exceeded. Remaining budget: {tracker.GetRemainingBudget(budgetContext)}");
            }

            // Log warning if approaching budget limit
            if (tracker.IsApproachingLimit(budgetContext))
            {
                _logger.LogWarning($"Privacy budget approaching limit for context '{budgetContext}'. Consumed: {tracker.GetConsumedBudget(budgetContext)}, Remaining: {tracker.GetRemainingBudget(budgetContext)}");
            }

            // Find the value node to perturb
            ElementNode valueNode = GetValueNode(node);

            if (valueNode?.Value == null)
            {
                return result;
            }

            // Apply differential privacy noise
            ApplyDifferentialPrivacyNoise(valueNode, dpSetting);

            // Mark nodes as visited
            if (context?.VisitedNodes != null)
            {
                context.VisitedNodes.UnionWith(node.Descendants().CastElementNodes());
            }

            result.AddProcessRecord(AnonymizationOperations.Perturb, node);
            result.AddProcessRecord(AnonymizationOperations.DifferentialPrivacy, node);
            result.AddPrivacyMetric("epsilon-consumed", dpSetting.Epsilon);
            result.AddPrivacyMetric("delta", dpSetting.Delta);
            result.AddPrivacyMetric("mechanism", dpSetting.Mechanism.ToString());
            result.AddPrivacyMetric("budget-context", budgetContext);
            result.AddPrivacyMetric("total-epsilon-consumed", tracker.GetConsumedBudget(budgetContext));
            result.AddPrivacyMetric("remaining-budget", tracker.GetRemainingBudget(budgetContext));

            return result;
        }

        private ElementNode GetValueNode(ElementNode node)
        {
            // If the node itself is numeric, use it
            if (s_numericTypeNames.Contains(node.InstanceType, StringComparer.InvariantCultureIgnoreCase))
            {
                return node;
            }

            // Otherwise, try to find a value child node (e.g., Quantity.value)
            var valueChild = node.Children(Constants.ValueNodeName).FirstOrDefault() as ElementNode;
            if (valueChild != null && s_numericTypeNames.Contains(valueChild.InstanceType, StringComparer.InvariantCultureIgnoreCase))
            {
                return valueChild;
            }

            return null;
        }

        private void ApplyDifferentialPrivacyNoise(ElementNode node, DifferentialPrivacySetting setting)
        {
            if (node?.Value == null)
            {
                return;
            }

            // Parse original value
            if (!decimal.TryParse(node.Value.ToString(), out decimal originalValue))
            {
                _logger.LogWarning($"Cannot apply differential privacy to non-numeric value: {node.Value}");
                return;
            }

            // Determine if integer type
            bool isInteger = s_integerTypeNames.Contains(node.InstanceType, StringComparer.InvariantCultureIgnoreCase);

            // Generate noise based on mechanism
            double noise = GenerateNoise(setting);

            // Add noise to original value with overflow handling
            decimal noisyValue;
            try
            {
                noisyValue = originalValue + (decimal)noise;
            }
            catch (OverflowException)
            {
                // Clamp to decimal min/max on overflow
                noisyValue = noise > 0 ? decimal.MaxValue : decimal.MinValue;
                _logger.LogWarning($"Overflow when applying noise. Clamped to {noisyValue}");
            }

            // Round if integer type
            if (isInteger)
            {
                noisyValue = Math.Round(noisyValue);
            }

            // Apply constraints for specific types
            if (string.Equals(FHIRAllTypes.PositiveInt.ToString(), node.InstanceType, StringComparison.InvariantCultureIgnoreCase))
            {
                noisyValue = Math.Max(1, noisyValue);
            }
            else if (string.Equals(FHIRAllTypes.UnsignedInt.ToString(), node.InstanceType, StringComparison.InvariantCultureIgnoreCase))
            {
                noisyValue = Math.Max(0, noisyValue);
            }

            node.Value = noisyValue;
        }

        /// <summary>
        /// Generate noise according to the specified mechanism using cryptographically secure RNG
        /// SECURITY: Uses System.Security.Cryptography.RandomNumberGenerator for cryptographic guarantees
        /// </summary>
        private double GenerateNoise(DifferentialPrivacySetting setting)
        {
            switch (setting.Mechanism)
            {
                case DPMechanism.Laplace:
                    // Laplace mechanism: noise ~ Laplace(0, sensitivity/epsilon)
                    var scale = setting.Sensitivity / setting.Epsilon;
                    return SampleLaplace(0, scale);

                case DPMechanism.Gaussian:
                    // Gaussian mechanism: noise ~ N(0, (sensitivity * sqrt(2*ln(1.25/delta)) / epsilon)^2)
                    // This provides (epsilon, delta)-differential privacy
                    if (setting.Delta <= 0)
                    {
                        throw new InvalidOperationException("Gaussian mechanism requires delta > 0");
                    }
                    var stdDev = setting.Sensitivity * Math.Sqrt(2 * Math.Log(1.25 / setting.Delta)) / setting.Epsilon;
                    return SampleGaussian(0, stdDev);

                case DPMechanism.Exponential:
                    // Exponential mechanism - for categorical data, but we'll use it for numeric as well
                    // Sample proportional to exp(-epsilon * |value - original| / (2*sensitivity))
                    // For simplicity, we'll use Laplace for now
                    var expScale = setting.Sensitivity / setting.Epsilon;
                    return SampleLaplace(0, expScale);

                default:
                    throw new ArgumentException($"Unknown differential privacy mechanism: {setting.Mechanism}");
            }
        }

        /// <summary>
        /// Sample from Laplace distribution using cryptographically secure RNG
        /// Laplace(mu, b) uses inverse CDF: mu - b * sgn(u) * ln(1 - 2|u|) where u ~ Uniform(-0.5, 0.5)
        /// SECURITY: Uses System.Security.Cryptography.RandomNumberGenerator
        /// </summary>
        private double SampleLaplace(double location, double scale)
        {
            // Generate cryptographically secure uniform random value in [0, 1)
            double u = SampleUniform() - 0.5;  // Convert to (-0.5, 0.5)
            
            // Apply Laplace inverse CDF
            double sign = u < 0 ? -1.0 : 1.0;
            return location - scale * sign * Math.Log(1 - 2 * Math.Abs(u));
        }

        /// <summary>
        /// Sample from Gaussian (Normal) distribution using cryptographically secure RNG
        /// Uses Box-Muller transform: if U1, U2 ~ Uniform(0,1), then
        /// X = sqrt(-2*ln(U1)) * cos(2*pi*U2) ~ N(0,1)
        /// SECURITY: Uses System.Security.Cryptography.RandomNumberGenerator
        /// </summary>
        private double SampleGaussian(double mean, double stdDev)
        {
            // Box-Muller transform
            double u1 = SampleUniform();
            double u2 = SampleUniform();
            
            // Ensure u1 > 0 to avoid log(0)
            if (u1 < 1e-10) u1 = 1e-10;
            
            double z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            return mean + stdDev * z0;
        }

        /// <summary>
        /// Generate cryptographically secure uniform random value in [0, 1)
        /// SECURITY CRITICAL: Uses System.Security.Cryptography.RandomNumberGenerator for true randomness
        /// This is essential for differential privacy guarantees - System.Random is NOT sufficient
        /// </summary>
        private double SampleUniform()
        {
            byte[] bytes = new byte[8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            
            // Convert to ulong and normalize to [0, 1)
            ulong randomULong = BitConverter.ToUInt64(bytes, 0);
            return (double)randomULong / (double)ulong.MaxValue;
        }
    }
}
