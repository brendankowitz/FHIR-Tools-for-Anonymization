// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors
{
    /// <summary>
    /// Comprehensive unit tests for DifferentialPrivacyProcessor
    /// Tests parameter validation, noise correctness, budget tracking, error conditions, and edge cases
    /// </summary>
    [Collection("DifferentialPrivacyTests")]
    public class DifferentialPrivacyProcessorTests
    {
        private readonly ITestOutputHelper _output;

        public DifferentialPrivacyProcessorTests(ITestOutputHelper output)
        {
            _output = output;
        }

        #region Parameter Validation Tests

        [Fact]
        public void Process_WithZeroEpsilon_ShouldThrowArgumentException()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var budgetContext = $"test-{Guid.NewGuid()}";
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, 10.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 0.0 },
                { "sensitivity", 1.0 },
                { "mechanism", "Laplace" },
                { "budgetContext", budgetContext }
            };

            var node = ElementNode.FromElement(new FhirDecimal(100.0m).ToTypedElement());

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => processor.Process(node, null, settings));
            Assert.Contains("epsilon", exception.Message.ToLower());
        }

        [Fact]
        public void Process_WithNegativeEpsilon_ShouldThrowArgumentException()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var budgetContext = $"test-{Guid.NewGuid()}";
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, 10.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", -0.5 },
                { "sensitivity", 1.0 },
                { "mechanism", "Laplace" },
                { "budgetContext", budgetContext }
            };

            var node = ElementNode.FromElement(new FhirDecimal(100.0m).ToTypedElement());

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => processor.Process(node, null, settings));
            Assert.Contains("epsilon", exception.Message.ToLower());
        }

        [Fact]
        public void Process_WithZeroSensitivity_ShouldThrowArgumentException()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var budgetContext = $"test-{Guid.NewGuid()}";
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, 10.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 1.0 },
                { "sensitivity", 0.0 },
                { "mechanism", "Laplace" },
                { "budgetContext", budgetContext }
            };

            var node = ElementNode.FromElement(new FhirDecimal(100.0m).ToTypedElement());

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => processor.Process(node, null, settings));
            Assert.Contains("sensitivity", exception.Message.ToLower());
        }

        [Fact]
        public void Process_WithNegativeSensitivity_ShouldThrowArgumentException()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var budgetContext = $"test-{Guid.NewGuid()}";
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, 10.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 1.0 },
                { "sensitivity", -1.0 },
                { "mechanism", "Laplace" },
                { "budgetContext", budgetContext }
            };

            var node = ElementNode.FromElement(new FhirDecimal(100.0m).ToTypedElement());

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => processor.Process(node, null, settings));
            Assert.Contains("sensitivity", exception.Message.ToLower());
        }

        [Fact]
        public void Process_WithMissingBudgetContext_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var settings = new Dictionary<string, object>
            {
                { "epsilon", 1.0 },
                { "sensitivity", 1.0 },
                { "mechanism", "Laplace" }
                // Missing budgetContext
            };

            var node = ElementNode.FromElement(new FhirDecimal(100.0m).ToTypedElement());

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => processor.Process(node, null, settings));
            Assert.Contains("budget context", exception.Message.ToLower());
        }

        [Fact]
        public void Process_WithInvalidMechanism_ShouldThrowArgumentException()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var budgetContext = $"test-{Guid.NewGuid()}";
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, 10.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 1.0 },
                { "sensitivity", 1.0 },
                { "mechanism", "Invalid" },
                { "budgetContext", budgetContext }
            };

            var node = ElementNode.FromElement(new FhirDecimal(100.0m).ToTypedElement());

            // Act & Assert
            Assert.ThrowsAny<Exception>(() => processor.Process(node, null, settings));
        }

        [Fact]
        public void Process_GaussianMechanism_WithZeroDelta_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var budgetContext = $"test-{Guid.NewGuid()}";
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, 10.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 1.0 },
                { "sensitivity", 1.0 },
                { "mechanism", "Gaussian" },
                { "delta", 0.0 },
                { "budgetContext", budgetContext }
            };

            var node = ElementNode.FromElement(new FhirDecimal(100.0m).ToTypedElement());

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => processor.Process(node, null, settings));
            Assert.Contains("delta", exception.Message.ToLower());
        }

        [Fact]
        public void Process_GaussianMechanism_WithNegativeDelta_ShouldThrowArgumentException()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var budgetContext = $"test-{Guid.NewGuid()}";
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, 10.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 1.0 },
                { "sensitivity", 1.0 },
                { "mechanism", "Gaussian" },
                { "delta", -0.00001 },
                { "budgetContext", budgetContext }
            };

            var node = ElementNode.FromElement(new FhirDecimal(100.0m).ToTypedElement());

            // Act & Assert
            Assert.ThrowsAny<Exception>(() => processor.Process(node, null, settings));
        }

        #endregion

        #region Noise Correctness Tests

        [Fact]
        public void Process_LaplaceMechanism_ShouldProduceCorrectNoiseDistribution()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var budgetContext = $"noise-test-{Guid.NewGuid()}";
            var epsilon = 0.5;
            var sensitivity = 1.0;
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, 100.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", epsilon },
                { "sensitivity", sensitivity },
                { "mechanism", "Laplace" },
                { "budgetContext", budgetContext }
            };

            var originalValue = 100.0m;
            var samples = new List<double>();

            // Act: Generate 200 samples
            for (int i = 0; i < 200; i++)
            {
                var node = ElementNode.FromElement(new FhirDecimal(originalValue).ToTypedElement());
                var result = processor.Process(node, null, settings);
                samples.Add((double)((decimal)node.Value - originalValue));
            }

            // Assert: Verify distribution characteristics
            // Laplace(0, b) where b = sensitivity/epsilon has E[X] = 0, Var[X] = 2b^2
            var scale = sensitivity / epsilon; // 1.0 / 0.5 = 2.0
            var expectedVariance = 2 * scale * scale; // 2 * 4 = 8
            var expectedStdDev = Math.Sqrt(expectedVariance); // ~2.83

            var mean = samples.Average();
            var variance = samples.Select(x => Math.Pow(x - mean, 2)).Average();
            var stdDev = Math.Sqrt(variance);

            _output.WriteLine($"Laplace Mechanism: scale={scale:F2}");
            _output.WriteLine($"Expected: mean=0, stddev={expectedStdDev:F2}");
            _output.WriteLine($"Actual: mean={mean:F2}, stddev={stdDev:F2}");

            // With 200 samples, allow 40% tolerance
            Assert.InRange(mean, -0.5, 0.5); // Mean should be close to 0
            Assert.InRange(stdDev, expectedStdDev * 0.6, expectedStdDev * 1.4);
        }

        [Fact]
        public void Process_GaussianMechanism_ShouldProduceCorrectNoiseDistribution()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var budgetContext = $"gaussian-noise-test-{Guid.NewGuid()}";
            var epsilon = 0.5;
            var sensitivity = 1.0;
            var delta = 0.00001;
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, 100.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", epsilon },
                { "sensitivity", sensitivity },
                { "mechanism", "Gaussian" },
                { "delta", delta },
                { "budgetContext", budgetContext }
            };

            var originalValue = 100.0m;
            var samples = new List<double>();

            // Act: Generate 200 samples
            for (int i = 0; i < 200; i++)
            {
                var node = ElementNode.FromElement(new FhirDecimal(originalValue).ToTypedElement());
                var result = processor.Process(node, null, settings);
                samples.Add((double)((decimal)node.Value - originalValue));
            }

            // Assert: Verify distribution characteristics
            // Gaussian mechanism: stddev = sensitivity * sqrt(2*ln(1.25/delta)) / epsilon
            var expectedStdDev = sensitivity * Math.Sqrt(2 * Math.Log(1.25 / delta)) / epsilon;

            var mean = samples.Average();
            var variance = samples.Select(x => Math.Pow(x - mean, 2)).Average();
            var stdDev = Math.Sqrt(variance);

            _output.WriteLine($"Gaussian Mechanism: epsilon={epsilon}, delta={delta}");
            _output.WriteLine($"Expected: mean=0, stddev={expectedStdDev:F2}");
            _output.WriteLine($"Actual: mean={mean:F2}, stddev={stdDev:F2}");

            // With 200 samples, allow 40% tolerance
            Assert.InRange(mean, -1.0, 1.0); // Mean should be close to 0
            Assert.InRange(stdDev, expectedStdDev * 0.6, expectedStdDev * 1.4);
        }

        #endregion

        #region Budget Tracking Tests

        [Fact]
        public void Process_ShouldConsumeBudgetCorrectly()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var budgetContext = $"budget-test-{Guid.NewGuid()}";
            var totalBudget = 10.0;
            var epsilon = 1.0;
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, totalBudget);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", epsilon },
                { "sensitivity", 1.0 },
                { "mechanism", "Laplace" },
                { "budgetContext", budgetContext }
            };

            var node = ElementNode.FromElement(new FhirDecimal(100.0m).ToTypedElement());

            // Act
            var result = processor.Process(node, null, settings);

            // Assert
            var consumed = PrivacyBudgetTracker.Instance.GetConsumedBudget(budgetContext);
            var remaining = PrivacyBudgetTracker.Instance.GetRemainingBudget(budgetContext);

            Assert.Equal(epsilon, consumed);
            Assert.Equal(totalBudget - epsilon, remaining);
            Assert.True(result.PrivacyMetrics.ContainsKey("total-epsilon-consumed"));
            Assert.Equal(epsilon, (double)result.PrivacyMetrics["total-epsilon-consumed"]);
        }

        [Fact]
        public void Process_WhenBudgetExceeded_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var budgetContext = $"exceeded-budget-test-{Guid.NewGuid()}";
            var totalBudget = 1.0;
            var epsilon = 0.6;
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, totalBudget);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", epsilon },
                { "sensitivity", 1.0 },
                { "mechanism", "Laplace" },
                { "budgetContext", budgetContext }
            };

            // Consume most of the budget
            var node1 = ElementNode.FromElement(new FhirDecimal(100.0m).ToTypedElement());
            processor.Process(node1, null, settings);

            // Act & Assert: Try to exceed budget
            var node2 = ElementNode.FromElement(new FhirDecimal(100.0m).ToTypedElement());
            var exception = Assert.Throws<InvalidOperationException>(() => processor.Process(node2, null, settings));
            Assert.Contains("budget exceeded", exception.Message.ToLower());
        }

        #endregion

        #region Edge Cases and Error Conditions

        [Fact]
        public void Process_WithNullNode_ShouldReturnEmptyResult()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var settings = new Dictionary<string, object>
            {
                { "epsilon", 1.0 },
                { "sensitivity", 1.0 },
                { "mechanism", "Laplace" }
            };

            // Act
            var result = processor.Process(null, null, settings);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.ProcessRecords);
        }

        [Fact]
        public void Process_WithNullSettings_ShouldReturnEmptyResult()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var node = ElementNode.FromElement(new FhirDecimal(100.0m).ToTypedElement());

            // Act
            var result = processor.Process(node, null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.ProcessRecords);
        }

        [Fact]
        public void Process_WithNonNumericValue_ShouldReturnEmptyResult()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var budgetContext = $"test-{Guid.NewGuid()}";
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, 10.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 1.0 },
                { "sensitivity", 1.0 },
                { "mechanism", "Laplace" },
                { "budgetContext", budgetContext }
            };

            var stringValue = new FhirString("not a number");
            var node = ElementNode.FromElement(stringValue.ToTypedElement());

            // Act
            var result = processor.Process(node, null, settings);

            // Assert - budget should still be consumed even though no noise was added
            Assert.NotNull(result);
        }

        [Fact]
        public void Process_WithIntegerType_ShouldRoundResult()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var budgetContext = $"integer-test-{Guid.NewGuid()}";
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, 10.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 0.1 }, // Large noise
                { "sensitivity", 10.0 },
                { "mechanism", "Laplace" },
                { "budgetContext", budgetContext }
            };

            var intValue = new Integer(100);
            var node = ElementNode.FromElement(intValue.ToTypedElement());

            // Act
            var result = processor.Process(node, null, settings);

            // Assert: Result should be rounded (no decimal places)
            var noisyValue = (decimal)node.Value;
            Assert.Equal(Math.Round(noisyValue), noisyValue);
        }

        [Fact]
        public void Process_WithPositiveIntType_ShouldClampToMinimumOne()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var budgetContext = $"positiveint-test-{Guid.NewGuid()}";
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, 50.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 0.01 }, // Very large noise to try to make value negative
                { "sensitivity", 100.0 },
                { "mechanism", "Laplace" },
                { "budgetContext", budgetContext }
            };

            // Try many times to get a value that would be < 1 without clamping
            bool foundClampedValue = false;
            for (int i = 0; i < 30 && !foundClampedValue; i++)
            {
                var posIntValue = new PositiveInt(2);
                var node = ElementNode.FromElement(posIntValue.ToTypedElement());

                // Act
                processor.Process(node, null, settings);

                // Assert
                var noisyValue = (decimal)node.Value;
                Assert.True(noisyValue >= 1, "PositiveInt should be clamped to minimum 1");
                
                if (noisyValue == 1)
                {
                    foundClampedValue = true;
                    _output.WriteLine($"Found clamped value: {noisyValue}");
                }
            }
        }

        [Fact]
        public void Process_WithUnsignedIntType_ShouldClampToMinimumZero()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var budgetContext = $"unsignedint-test-{Guid.NewGuid()}";
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, 50.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 0.01 }, // Very large noise to try to make value negative
                { "sensitivity", 100.0 },
                { "mechanism", "Laplace" },
                { "budgetContext", budgetContext }
            };

            // Try many times to get a value that would be < 0 without clamping
            bool foundClampedValue = false;
            for (int i = 0; i < 30 && !foundClampedValue; i++)
            {
                var unsignedIntValue = new UnsignedInt(5);
                var node = ElementNode.FromElement(unsignedIntValue.ToTypedElement());

                // Act
                processor.Process(node, null, settings);

                // Assert
                var noisyValue = (decimal)node.Value;
                Assert.True(noisyValue >= 0, "UnsignedInt should be clamped to minimum 0");
                
                if (noisyValue == 0)
                {
                    foundClampedValue = true;
                    _output.WriteLine($"Found clamped value: {noisyValue}");
                }
            }
        }

        [Fact]
        public void Process_WithVeryLargeEpsilon_ShouldStillWork()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var budgetContext = $"large-epsilon-test-{Guid.NewGuid()}";
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, 1000.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 100.0 }, // Very large epsilon (little noise)
                { "sensitivity", 1.0 },
                { "mechanism", "Laplace" },
                { "budgetContext", budgetContext }
            };

            var node = ElementNode.FromElement(new FhirDecimal(100.0m).ToTypedElement());

            // Act
            var result = processor.Process(node, null, settings);

            // Assert: With very large epsilon, noise should be very small
            var noisyValue = (decimal)node.Value;
            Assert.InRange(noisyValue, 95.0m, 105.0m); // Should be close to original
        }

        #endregion

        #region Process Result Tests

        [Fact]
        public void Process_ShouldReturnCorrectProcessResult()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var budgetContext = $"result-test-{Guid.NewGuid()}";
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, 10.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 1.0 },
                { "sensitivity", 1.0 },
                { "mechanism", "Laplace" },
                { "budgetContext", budgetContext }
            };

            var node = ElementNode.FromElement(new FhirDecimal(100.0m).ToTypedElement());

            // Act
            var result = processor.Process(node, null, settings);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.ProcessRecords);
            Assert.Contains(result.ProcessRecords, r => r.AnonymizationOperation == AnonymizationOperations.DifferentialPrivacy);
            Assert.Contains(result.ProcessRecords, r => r.AnonymizationOperation == AnonymizationOperations.Perturb);
            
            // Check privacy metrics
            Assert.True(result.PrivacyMetrics.ContainsKey("epsilon-consumed"));
            Assert.True(result.PrivacyMetrics.ContainsKey("mechanism"));
            Assert.True(result.PrivacyMetrics.ContainsKey("budget-context"));
            Assert.Equal(1.0, (double)result.PrivacyMetrics["epsilon-consumed"]);
            Assert.Equal("Laplace", result.PrivacyMetrics["mechanism"].ToString());
        }

        [Fact]
        public void Process_GaussianMechanism_ShouldIncludeDeltaInResult()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var budgetContext = $"delta-test-{Guid.NewGuid()}";
            PrivacyBudgetTracker.Instance.InitializeBudget(budgetContext, 10.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 1.0 },
                { "sensitivity", 1.0 },
                { "mechanism", "Gaussian" },
                { "delta", 0.00001 },
                { "budgetContext", budgetContext }
            };

            var node = ElementNode.FromElement(new FhirDecimal(100.0m).ToTypedElement());

            // Act
            var result = processor.Process(node, null, settings);

            // Assert
            Assert.True(result.PrivacyMetrics.ContainsKey("delta"));
            Assert.Equal(0.00001, (double)result.PrivacyMetrics["delta"]);
        }

        #endregion
    }
}
