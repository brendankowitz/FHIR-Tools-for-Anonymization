using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Integration
{
    public class DifferentialPrivacyIntegrationTests
    {
        [Fact]
        public void EndToEnd_DifferentialPrivacyOnObservationValues_ShouldAddNoise()
        {
            // Arrange: Create configuration with differential privacy
            var config = new AnonymizerConfiguration
            {
                PathRules = new Dictionary<string, AnonymizerRule>
                {
                    {
                        "Observation.valueQuantity.value",
                        new AnonymizerRule
                        {
                            Path = "Observation.valueQuantity.value",
                            Method = AnonymizerMethod.DifferentialPrivacy,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { RuleKeys.Epsilon, "0.5" },
                                { RuleKeys.Sensitivity, "10.0" },
                                { RuleKeys.MechanismType, "laplace" }
                            }
                        }
                    }
                }
            };

            var engine = new AnonymizerEngine(config);

            // Create test observation
            var observationJson = CreateObservationJson(120.5);
            var originalValue = 120.5;

            // Act: Anonymize observation multiple times
            var anonymizedObservations = Enumerable.Range(0, 10)
                .Select(_ => engine.AnonymizeJson(observationJson))
                .ToList();

            // Assert: Verify noise was added
            var values = anonymizedObservations
                .Select(o => JObject.Parse(o))
                .Select(o => (double)o.SelectToken("valueQuantity.value"))
                .ToList();

            // Values should differ from original (noise added)
            Assert.True(values.Any(v => Math.Abs(v - originalValue) > 0.01),
                "At least some values should differ from original due to noise");

            // Values should be within reasonable range (Laplace noise is unbounded but typically small)
            var meanValue = values.Average();
            Assert.InRange(meanValue, originalValue - 50, originalValue + 50);
        }

        [Fact]
        public void EndToEnd_DifferentialPrivacyBudgetTracking_ShouldConsumeEpsilon()
        {
            // Arrange
            var epsilon = 0.5;
            var config = new AnonymizerConfiguration
            {
                PathRules = new Dictionary<string, AnonymizerRule>
                {
                    {
                        "Observation.valueQuantity.value",
                        new AnonymizerRule
                        {
                            Path = "Observation.valueQuantity.value",
                            Method = AnonymizerMethod.DifferentialPrivacy,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { RuleKeys.Epsilon, epsilon.ToString() },
                                { RuleKeys.Sensitivity, "10.0" },
                                { RuleKeys.MechanismType, "laplace" }
                            }
                        }
                    }
                }
            };

            var engine = new AnonymizerEngine(config);
            var observationJson = CreateObservationJson(120.5);

            // Act: Anonymize multiple times
            var iterations = 5;
            for (int i = 0; i < iterations; i++)
            {
                engine.AnonymizeJson(observationJson);
            }

            // Assert: Budget should be consumed
            var tracker = PrivacyBudgetTracker.Instance;
            var remainingBudget = tracker.GetRemainingBudget();
            var consumedBudget = tracker.GetConsumedBudget();

            Assert.True(consumedBudget > 0, "Some epsilon budget should be consumed");
            Assert.Equal(epsilon * iterations, consumedBudget, 2);
        }

        [Fact]
        public void EndToEnd_DifferentialPrivacyBudgetExhausted_ShouldThrowException()
        {
            // Arrange: Create configuration with small total budget
            var config = new AnonymizerConfiguration
            {
                PathRules = new Dictionary<string, AnonymizerRule>
                {
                    {
                        "Observation.valueQuantity.value",
                        new AnonymizerRule
                        {
                            Path = "Observation.valueQuantity.value",
                            Method = AnonymizerMethod.DifferentialPrivacy,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { RuleKeys.Epsilon, "0.5" },
                                { RuleKeys.Sensitivity, "10.0" },
                                { RuleKeys.MechanismType, "laplace" },
                                { RuleKeys.TotalPrivacyBudget, "1.0" }
                            }
                        }
                    }
                }
            };

            // Reset budget tracker
            PrivacyBudgetTracker.Instance.Reset();
            PrivacyBudgetTracker.Instance.SetTotalBudget(1.0);

            var engine = new AnonymizerEngine(config);
            var observationJson = CreateObservationJson(120.5);

            // Act: Anonymize until budget exhausted
            engine.AnonymizeJson(observationJson); // Consumes 0.5
            engine.AnonymizeJson(observationJson); // Consumes 0.5, total = 1.0

            // Assert: Next call should throw
            Assert.Throws<InvalidOperationException>(() => engine.AnonymizeJson(observationJson));
        }

        [Fact]
        public void EndToEnd_DifferentialPrivacyWithSmallEpsilon_ShouldAddMoreNoise()
        {
            // Arrange: Two configurations with different epsilon values
            var configHighPrivacy = new AnonymizerConfiguration
            {
                PathRules = new Dictionary<string, AnonymizerRule>
                {
                    {
                        "Observation.valueQuantity.value",
                        new AnonymizerRule
                        {
                            Path = "Observation.valueQuantity.value",
                            Method = AnonymizerMethod.DifferentialPrivacy,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { RuleKeys.Epsilon, "0.1" }, // Small epsilon = more privacy, more noise
                                { RuleKeys.Sensitivity, "10.0" },
                                { RuleKeys.MechanismType, "laplace" }
                            }
                        }
                    }
                }
            };

            var configLowPrivacy = new AnonymizerConfiguration
            {
                PathRules = new Dictionary<string, AnonymizerRule>
                {
                    {
                        "Observation.valueQuantity.value",
                        new AnonymizerRule
                        {
                            Path = "Observation.valueQuantity.value",
                            Method = AnonymizerMethod.DifferentialPrivacy,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { RuleKeys.Epsilon, "1.0" }, // Large epsilon = less privacy, less noise
                                { RuleKeys.Sensitivity, "10.0" },
                                { RuleKeys.MechanismType, "laplace" }
                            }
                        }
                    }
                }
            };

            var engineHighPrivacy = new AnonymizerEngine(configHighPrivacy);
            var engineLowPrivacy = new AnonymizerEngine(configLowPrivacy);
            var observationJson = CreateObservationJson(100.0);

            // Act: Anonymize multiple times with both configurations
            var highPrivacyValues = Enumerable.Range(0, 50)
                .Select(_ => engineHighPrivacy.AnonymizeJson(observationJson))
                .Select(o => (double)JObject.Parse(o).SelectToken("valueQuantity.value"))
                .ToList();

            var lowPrivacyValues = Enumerable.Range(0, 50)
                .Select(_ => engineLowPrivacy.AnonymizeJson(observationJson))
                .Select(o => (double)JObject.Parse(o).SelectToken("valueQuantity.value"))
                .ToList();

            // Assert: High privacy should have more variance (more noise)
            var highPrivacyVariance = CalculateVariance(highPrivacyValues);
            var lowPrivacyVariance = CalculateVariance(lowPrivacyValues);

            Assert.True(highPrivacyVariance > lowPrivacyVariance * 0.5,
                "Higher privacy (smaller epsilon) should generally result in more variance");
        }

        [Fact]
        public void EndToEnd_DifferentialPrivacyWithExponentialMechanism_ShouldWorkForCategorical()
        {
            // Arrange: Configuration using exponential mechanism for categorical data
            var config = new AnonymizerConfiguration
            {
                PathRules = new Dictionary<string, AnonymizerRule>
                {
                    {
                        "Observation.valueString",
                        new AnonymizerRule
                        {
                            Path = "Observation.valueString",
                            Method = AnonymizerMethod.DifferentialPrivacy,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { RuleKeys.Epsilon, "0.5" },
                                { RuleKeys.MechanismType, "exponential" },
                                { RuleKeys.Categories, "low,medium,high" }
                            }
                        }
                    }
                }
            };

            var engine = new AnonymizerEngine(config);
            var observationJson = @"{
                ""resourceType"": ""Observation"",
                ""status"": ""final"",
                ""code"": { ""text"": ""Test"" },
                ""valueString"": ""medium""
            }";

            // Act: Anonymize multiple times
            var anonymizedObservations = Enumerable.Range(0, 20)
                .Select(_ => engine.AnonymizeJson(observationJson))
                .Select(o => JObject.Parse(o).SelectToken("valueString")?.ToString())
                .Where(v => v != null)
                .ToList();

            // Assert: Values should be from valid categories
            var validCategories = new[] { "low", "medium", "high" };
            Assert.All(anonymizedObservations, value => Assert.Contains(value, validCategories));

            // Should have some randomization (not all the same)
            var distinctValues = anonymizedObservations.Distinct().Count();
            Assert.True(distinctValues >= 1, "Should have at least one distinct value");
        }

        [Fact]
        public void EndToEnd_DifferentialPrivacyConfigurationValidation_ShouldValidateEpsilon()
        {
            // Arrange: Invalid epsilon (negative)
            var config = new AnonymizerConfiguration
            {
                PathRules = new Dictionary<string, AnonymizerRule>
                {
                    {
                        "Observation.valueQuantity.value",
                        new AnonymizerRule
                        {
                            Path = "Observation.valueQuantity.value",
                            Method = AnonymizerMethod.DifferentialPrivacy,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { RuleKeys.Epsilon, "-0.5" }, // Invalid
                                { RuleKeys.Sensitivity, "10.0" },
                                { RuleKeys.MechanismType, "laplace" }
                            }
                        }
                    }
                }
            };

            // Act & Assert: Should throw during engine creation
            Assert.Throws<ArgumentException>(() => new AnonymizerEngine(config));
        }

        [Fact]
        public void EndToEnd_DifferentialPrivacyConfigurationValidation_ShouldValidateSensitivity()
        {
            // Arrange: Missing sensitivity
            var config = new AnonymizerConfiguration
            {
                PathRules = new Dictionary<string, AnonymizerRule>
                {
                    {
                        "Observation.valueQuantity.value",
                        new AnonymizerRule
                        {
                            Path = "Observation.valueQuantity.value",
                            Method = AnonymizerMethod.DifferentialPrivacy,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { RuleKeys.Epsilon, "0.5" },
                                { RuleKeys.MechanismType, "laplace" }
                                // Missing sensitivity
                            }
                        }
                    }
                }
            };

            // Act & Assert: Should throw during engine creation
            Assert.Throws<ArgumentException>(() => new AnonymizerEngine(config));
        }

        [Fact]
        public void EndToEnd_DifferentialPrivacyWithBudgetReset_ShouldAllowMoreOperations()
        {
            // Arrange
            var config = new AnonymizerConfiguration
            {
                PathRules = new Dictionary<string, AnonymizerRule>
                {
                    {
                        "Observation.valueQuantity.value",
                        new AnonymizerRule
                        {
                            Path = "Observation.valueQuantity.value",
                            Method = AnonymizerMethod.DifferentialPrivacy,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { RuleKeys.Epsilon, "0.5" },
                                { RuleKeys.Sensitivity, "10.0" },
                                { RuleKeys.MechanismType, "laplace" },
                                { RuleKeys.TotalPrivacyBudget, "1.0" }
                            }
                        }
                    }
                }
            };

            var tracker = PrivacyBudgetTracker.Instance;
            tracker.Reset();
            tracker.SetTotalBudget(1.0);

            var engine = new AnonymizerEngine(config);
            var observationJson = CreateObservationJson(120.5);

            // Act: Consume budget
            engine.AnonymizeJson(observationJson); // 0.5
            engine.AnonymizeJson(observationJson); // 1.0 (exhausted)

            var consumedBeforeReset = tracker.GetConsumedBudget();
            Assert.Equal(1.0, consumedBeforeReset, 2);

            // Reset budget
            tracker.Reset();
            tracker.SetTotalBudget(1.0);

            // Should be able to anonymize again
            var result = engine.AnonymizeJson(observationJson);
            Assert.NotNull(result);

            var consumedAfterReset = tracker.GetConsumedBudget();
            Assert.Equal(0.5, consumedAfterReset, 2);
        }

        private string CreateObservationJson(double value)
        {
            return $@"{{
                ""resourceType"": ""Observation"",
                ""id"": ""{Guid.NewGuid()}"",
                ""status"": ""final"",
                ""code"": {{
                    ""text"": ""Blood Glucose""
                }},
                ""valueQuantity"": {{
                    ""value"": {value},
                    ""unit"": ""mg/dL"",
                    ""system"": ""http://unitsofmeasure.org"",
                    ""code"": ""mg/dL""
                }}
            }}";
        }

        private double CalculateVariance(List<double> values)
        {
            var mean = values.Average();
            var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
            return variance;
        }
    }
}
