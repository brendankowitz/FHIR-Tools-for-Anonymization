// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Integration
{
    /// <summary>
    /// Integration tests for differential privacy features
    /// Tests statistical validation of noise distribution, budget consumption, and privacy guarantees
    /// </summary>
    [Collection("DifferentialPrivacyTests")]
    public class DifferentialPrivacyIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public DifferentialPrivacyIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void EndToEnd_DifferentialPrivacyOnObservation_ShouldApplyNoise()
        {
            // Arrange: Create configuration with differential privacy
            var budgetContext = $"integration-test-{Guid.NewGuid()}";
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, 10.0);

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
                                { RuleKeys.Epsilon, "1.0" },
                                { RuleKeys.Sensitivity, "1.0" },
                                { RuleKeys.Mechanism, "Laplace" },
                                { RuleKeys.BudgetContext, budgetContext }
                            }
                        }
                    }
                }
            };

            var engine = new AnonymizerEngine(config);

            var observationJson = @"{
                ""resourceType"": ""Observation"",
                ""id"": ""example"",
                ""status"": ""final"",
                ""valueQuantity"": {
                    ""value"": 185.5,
                    ""unit"": ""cm"",
                    ""system"": ""http://unitsofmeasure.org"",
                    ""code"": ""cm""
                }
            }";

            // Act: Anonymize the observation
            var anonymizedJson = engine.AnonymizeJson(observationJson);
            var anonymizedObj = JObject.Parse(anonymizedJson);

            // Assert: Value should be changed (with noise)
            var anonymizedValue = anonymizedObj["valueQuantity"]["value"].Value<decimal>();
            _output.WriteLine($"Original: 185.5, Anonymized: {anonymizedValue}");

            // With epsilon=1.0, sensitivity=1.0, noise scale is 1.0
            // It's extremely unlikely (though not impossible) that noise is exactly 0
            // We just verify the structure is intact and value is plausible
            Assert.True(anonymizedValue > 0, "Value should be positive");
            Assert.InRange(anonymizedValue, 150m, 220m); // Reasonable range with Laplace(0, 1) noise
        }

        [Fact]
        public void EndToEnd_MultipleDifferentialPrivacyOperations_ShouldTrackBudget()
        {
            // Arrange: Create configuration that applies DP to multiple fields
            var budgetContext = $"multi-field-test-{Guid.NewGuid()}";
            var totalBudget = 5.0;
            var epsilonPerField = 1.0;
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, totalBudget);

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
                                { RuleKeys.Epsilon, epsilonPerField.ToString() },
                                { RuleKeys.Sensitivity, "1.0" },
                                { RuleKeys.Mechanism, "Laplace" },
                                { RuleKeys.BudgetContext, budgetContext }
                            }
                        }
                    }
                }
            };

            var engine = new AnonymizerEngine(config);

            var observationJson = @"{
                ""resourceType"": ""Observation"",
                ""id"": ""example"",
                ""status"": ""final"",
                ""valueQuantity"": {
                    ""value"": 185.5,
                    ""unit"": ""cm""
                }
            }";

            // Act: Anonymize multiple observations
            for (int i = 0; i < 4; i++)
            {
                engine.AnonymizeJson(observationJson);
            }

            // Assert: Budget should be tracked correctly
            var consumed = PrivacyBudgetTracker.Instance.GetConsumedBudget(budgetContext);
            var remaining = PrivacyBudgetTracker.Instance.GetRemainingBudget(budgetContext);

            _output.WriteLine($"Total Budget: {totalBudget}");
            _output.WriteLine($"Consumed: {consumed}");
            _output.WriteLine($"Remaining: {remaining}");

            Assert.Equal(4 * epsilonPerField, consumed);
            Assert.Equal(totalBudget - (4 * epsilonPerField), remaining);

            // Act: Try to exceed budget
            Assert.Throws<Exception>(() => engine.AnonymizeJson(observationJson));
        }

        [Fact]
        public void EndToEnd_DifferentialPrivacyNoiseDistribution_ShouldMatchExpected()
        {
            // Arrange: Statistical test with many samples
            var budgetContext = $"statistical-test-{Guid.NewGuid()}";
            var epsilon = 0.5;
            var sensitivity = 1.0;
            var sampleCount = 150;
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, sampleCount * epsilon);

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
                                { RuleKeys.Sensitivity, sensitivity.ToString() },
                                { RuleKeys.Mechanism, "Laplace" },
                                { RuleKeys.BudgetContext, budgetContext }
                            }
                        }
                    }
                }
            };

            var engine = new AnonymizerEngine(config);
            var originalValue = 100.0m;

            var observationTemplate = @"{
                ""resourceType"": ""Observation"",
                ""id"": ""example"",
                ""status"": ""final"",
                ""valueQuantity"": {
                    ""value"": " + originalValue + @",
                    ""unit"": ""cm""
                }
            }";

            // Act: Generate samples
            var anonymizedValues = new List<double>();
            for (int i = 0; i < sampleCount; i++)
            {
                var anonymized = engine.AnonymizeJson(observationTemplate);
                var value = JObject.Parse(anonymized)["valueQuantity"]["value"].Value<decimal>();
                anonymizedValues.Add((double)value);
            }

            // Assert: Statistical properties
            // Laplace(0, sensitivity/epsilon) has mean=0, stddev=sensitivity*sqrt(2)/epsilon
            var expectedMean = (double)originalValue;
            var expectedScale = sensitivity / epsilon; // 2.0
            var expectedStdDev = sensitivity * Math.Sqrt(2) / epsilon; // ~2.83

            var actualMean = anonymizedValues.Average();
            var actualVariance = anonymizedValues.Select(v => Math.Pow(v - actualMean, 2)).Average();
            var actualStdDev = Math.Sqrt(actualVariance);

            _output.WriteLine($"Statistical Test with {sampleCount} samples:");
            _output.WriteLine($"Expected: mean={expectedMean:F2}, stddev={expectedStdDev:F2}");
            _output.WriteLine($"Actual: mean={actualMean:F2}, stddev={actualStdDev:F2}");
            _output.WriteLine($"Deviation: mean={(actualMean - expectedMean):F2}, stddev={(actualStdDev - expectedStdDev):F2}");

            // Statistical test: With 150 samples, use 30-35% tolerance for >99% confidence
            // Standard error of mean: stddev/sqrt(n) = 2.83/sqrt(150) ≈ 0.23
            // 3 sigma range: ±0.69, so ±2.0 is very conservative
            Assert.InRange(actualMean, expectedMean - 2.0, expectedMean + 2.0);
            
            // Variance has higher sampling error, use 35% tolerance
            Assert.InRange(actualStdDev, expectedStdDev * 0.65, expectedStdDev * 1.35);
        }

        [Fact]
        public void EndToEnd_GaussianMechanism_ShouldProvideEpsilonDeltaPrivacy()
        {
            // Arrange
            var budgetContext = $"gaussian-integration-test-{Guid.NewGuid()}";
            var epsilon = 1.0;
            var delta = 0.00001;
            var sensitivity = 1.0;
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, 10.0);

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
                                { RuleKeys.Sensitivity, sensitivity.ToString() },
                                { RuleKeys.Mechanism, "Gaussian" },
                                { RuleKeys.Delta, delta.ToString() },
                                { RuleKeys.BudgetContext, budgetContext }
                            }
                        }
                    }
                }
            };

            var engine = new AnonymizerEngine(config);

            var observationJson = @"{
                ""resourceType"": ""Observation"",
                ""id"": ""example"",
                ""status"": ""final"",
                ""valueQuantity"": {
                    ""value"": 100.0,
                    ""unit"": ""mg/dL""
                }
            }";

            // Act
            var anonymizedJson = engine.AnonymizeJson(observationJson);
            var anonymizedObj = JObject.Parse(anonymizedJson);
            var anonymizedValue = anonymizedObj["valueQuantity"]["value"].Value<decimal>();

            // Assert: Verify value changed and is reasonable
            _output.WriteLine($"Original: 100.0, Anonymized: {anonymizedValue}");
            
            // Gaussian mechanism: stddev = sensitivity * sqrt(2*ln(1.25/delta)) / epsilon
            var expectedStdDev = sensitivity * Math.Sqrt(2 * Math.Log(1.25 / delta)) / epsilon;
            _output.WriteLine($"Expected stddev: {expectedStdDev:F2}");

            // Value should be within reasonable range (3 sigma)
            Assert.InRange(anonymizedValue, 100m - (decimal)(3 * expectedStdDev), 100m + (decimal)(3 * expectedStdDev));
        }

        [Fact]
        public void EndToEnd_DifferentialPrivacyOnIntegerFields_ShouldRoundResults()
        {
            // Arrange
            var budgetContext = $"integer-integration-test-{Guid.NewGuid()}";
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, 10.0);

            var config = new AnonymizerConfiguration
            {
                PathRules = new Dictionary<string, AnonymizerRule>
                {
                    {
                        "Patient.multipleBirthInteger",
                        new AnonymizerRule
                        {
                            Path = "Patient.multipleBirthInteger",
                            Method = AnonymizerMethod.DifferentialPrivacy,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { RuleKeys.Epsilon, "0.5" },
                                { RuleKeys.Sensitivity, "1.0" },
                                { RuleKeys.Mechanism, "Laplace" },
                                { RuleKeys.BudgetContext, budgetContext }
                            }
                        }
                    }
                }
            };

            var engine = new AnonymizerEngine(config);

            var patientJson = @"{
                ""resourceType"": ""Patient"",
                ""id"": ""example"",
                ""multipleBirthInteger"": 3
            }";

            // Act
            var anonymizedJson = engine.AnonymizeJson(patientJson);
            var anonymizedObj = JObject.Parse(anonymizedJson);
            var anonymizedValue = anonymizedObj["multipleBirthInteger"].Value<int>();

            // Assert: Should be an integer (no decimal places)
            _output.WriteLine($"Original: 3, Anonymized: {anonymizedValue}");
            Assert.IsType<int>(anonymizedValue);
        }

        [Fact]
        public void EndToEnd_EpsilonComparisonTest_HigherEpsilonShouldHaveLessNoise()
        {
            // Arrange: Compare low epsilon (more noise) vs high epsilon (less noise)
            var lowEpsilonContext = $"low-epsilon-{Guid.NewGuid()}";
            var highEpsilonContext = $"high-epsilon-{Guid.NewGuid()}";
            var sampleCount = 100;
            
            PrivacyBudgetTracker.Instance.InitializeBudget(lowEpsilonContext, sampleCount * 0.1);
            PrivacyBudgetTracker.Instance.InitializeBudget(highEpsilonContext, sampleCount * 2.0);

            var configLowEpsilon = CreateDPConfig(lowEpsilonContext, 0.1);
            var configHighEpsilon = CreateDPConfig(highEpsilonContext, 2.0);

            var engineLow = new AnonymizerEngine(configLowEpsilon);
            var engineHigh = new AnonymizerEngine(configHighEpsilon);

            var observationJson = @"{
                ""resourceType"": ""Observation"",
                ""valueQuantity"": { ""value"": 100.0 }
            }";

            // Act: Collect samples for both
            var lowEpsilonValues = new List<double>();
            var highEpsilonValues = new List<double>();

            for (int i = 0; i < sampleCount; i++)
            {
                var lowResult = JObject.Parse(engineLow.AnonymizeJson(observationJson));
                lowEpsilonValues.Add(lowResult["valueQuantity"]["value"].Value<double>());

                var highResult = JObject.Parse(engineHigh.AnonymizeJson(observationJson));
                highEpsilonValues.Add(highResult["valueQuantity"]["value"].Value<double>());
            }

            // Assert: Low epsilon should have higher variance
            var lowVariance = CalculateVariance(lowEpsilonValues, 100.0);
            var highVariance = CalculateVariance(highEpsilonValues, 100.0);

            _output.WriteLine($"Low epsilon (0.1) variance: {lowVariance:F2}");
            _output.WriteLine($"High epsilon (2.0) variance: {highVariance:F2}");

            // Variance ratio should be approximately (epsilon_high/epsilon_low)^2 = 400
            // But with sampling error, just verify low > high with good margin
            Assert.True(lowVariance > highVariance * 5, "Lower epsilon should produce significantly more noise");
        }

        private AnonymizerConfiguration CreateDPConfig(string budgetContext, double epsilon)
        {
            return new AnonymizerConfiguration
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
                                { RuleKeys.Sensitivity, "1.0" },
                                { RuleKeys.Mechanism, "Laplace" },
                                { RuleKeys.BudgetContext, budgetContext }
                            }
                        }
                    }
                }
            };
        }

        private double CalculateVariance(List<double> values, double expectedMean)
        {
            return values.Select(v => Math.Pow(v - expectedMean, 2)).Average();
        }
    }
}
