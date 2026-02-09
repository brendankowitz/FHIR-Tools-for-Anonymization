using System;
using System.Collections.Generic;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors
{
    public class DifferentialPrivacyProcessorTests
    {
        public DifferentialPrivacyProcessorTests()
        {
            // Reset privacy budget tracker before each test
            PrivacyBudgetTracker.Instance.ResetBudget("test-context");
            PrivacyBudgetTracker.Instance.ResetBudget("default");
        }

        [Fact]
        public void GivenValidDifferentialPrivacySetting_WhenInitialize_ProcessorShouldBeCreated()
        {
            var processor = new DifferentialPrivacyProcessor();
            Assert.NotNull(processor);
        }

        [Fact]
        public void GivenMissingEpsilon_WhenProcess_ShouldThrowException()
        {
            var settings = new Dictionary<string, object>
            {
                { "sensitivity", 1.0 }
            };

            var processor = new DifferentialPrivacyProcessor();
            var value = new FhirDecimal(100.5m);
            var node = ElementNode.FromElement(value.ToTypedElement());

            Assert.Throws<ArgumentException>(() => processor.Process(node, null, settings));
        }

        [Fact]
        public void GivenInvalidEpsilon_WhenProcess_ShouldThrowException()
        {
            var settings = new Dictionary<string, object>
            {
                { "epsilon", 0 },
                { "sensitivity", 1.0 }
            };

            var processor = new DifferentialPrivacyProcessor();
            var value = new FhirDecimal(100.5m);
            var node = ElementNode.FromElement(value.ToTypedElement());

            Assert.Throws<ArgumentException>(() => processor.Process(node, null, settings));
        }

        [Fact]
        public void GivenNegativeEpsilon_WhenProcess_ShouldThrowException()
        {
            var settings = new Dictionary<string, object>
            {
                { "epsilon", -0.5 },
                { "sensitivity", 1.0 }
            };

            var processor = new DifferentialPrivacyProcessor();
            var value = new FhirDecimal(100.5m);
            var node = ElementNode.FromElement(value.ToTypedElement());

            Assert.Throws<ArgumentException>(() => processor.Process(node, null, settings));
        }

        [Fact]
        public void GivenNumericNode_WhenProcessWithLaplaceNoise_ShouldAddNoise()
        {
            PrivacyBudgetTracker.Instance.SetTotalBudget("test-context", 1.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 0.1 },
                { "sensitivity", 1.0 },
                { "mechanism", "Laplace" },
                { "budgetContext", "test-context" }
            };

            var processor = new DifferentialPrivacyProcessor();
            var originalValue = 100.0m;
            var value = new FhirDecimal(originalValue);
            var node = ElementNode.FromElement(value.ToTypedElement());
            var result = processor.Process(node, null, settings);

            Assert.True(result.IsDifferentiallyPrivate);
            
            // The value should have been modified (with very high probability)
            var noisyValue = decimal.Parse(node.Value.ToString());
            Assert.NotEqual(originalValue, noisyValue);
        }

        [Fact]
        public void GivenIntegerNode_WhenProcess_ShouldPreserveIntegerType()
        {
            PrivacyBudgetTracker.Instance.SetTotalBudget("test-context", 1.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 0.5 },
                { "sensitivity", 1.0 },
                { "mechanism", "Laplace" },
                { "budgetContext", "test-context" }
            };

            var processor = new DifferentialPrivacyProcessor();
            var age = new Integer(45);
            var node = ElementNode.FromElement(age.ToTypedElement());
            var result = processor.Process(node, null, settings);

            Assert.True(result.IsDifferentiallyPrivate);
            
            // Should still be parseable as integer (rounded)
            var noisyValue = node.Value.ToString();
            Assert.True(int.TryParse(noisyValue, out _));
        }

        [Fact]
        public void GivenGaussianMechanism_WhenProcess_ShouldApplyGaussianNoise()
        {
            PrivacyBudgetTracker.Instance.SetTotalBudget("test-context", 1.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 0.1 },
                { "delta", 0.00001 },
                { "sensitivity", 1.0 },
                { "mechanism", "Gaussian" },
                { "budgetContext", "test-context" }
            };

            var processor = new DifferentialPrivacyProcessor();
            var originalValue = 75.5m;
            var value = new FhirDecimal(originalValue);
            var node = ElementNode.FromElement(value.ToTypedElement());
            var result = processor.Process(node, null, settings);

            Assert.True(result.IsDifferentiallyPrivate);
            
            // The value should have been modified
            var noisyValue = decimal.Parse(node.Value.ToString());
            Assert.NotEqual(originalValue, noisyValue);
        }

        [Fact]
        public void GivenBudgetExceeded_WhenProcess_ShouldThrowException()
        {
            PrivacyBudgetTracker.Instance.SetTotalBudget("test-context", 0.1);
            PrivacyBudgetTracker.Instance.ConsumeBudget("test-context", 0.1); // Consume all budget

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 0.5 },
                { "sensitivity", 1.0 },
                { "budgetContext", "test-context" }
            };

            var processor = new DifferentialPrivacyProcessor();
            var value = new FhirDecimal(100m);
            var node = ElementNode.FromElement(value.ToTypedElement());

            Assert.Throws<InvalidOperationException>(() => processor.Process(node, null, settings));
        }

        [Fact]
        public void GivenMultipleOperations_WhenProcess_ShouldTrackCumulativeBudget()
        {
            PrivacyBudgetTracker.Instance.SetTotalBudget("test-context", 1.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 0.3 },
                { "sensitivity", 1.0 },
                { "budgetContext", "test-context" }
            };

            var processor = new DifferentialPrivacyProcessor();

            // First operation
            var value1 = new FhirDecimal(100m);
            var node1 = ElementNode.FromElement(value1.ToTypedElement());
            var result1 = processor.Process(node1, null, settings);

            Assert.True(result1.IsDifferentiallyPrivate);
            Assert.Equal(0.3, PrivacyBudgetTracker.Instance.GetConsumedBudget("test-context"), 2);

            // Second operation
            var value2 = new FhirDecimal(200m);
            var node2 = ElementNode.FromElement(value2.ToTypedElement());
            var result2 = processor.Process(node2, null, settings);

            Assert.True(result2.IsDifferentiallyPrivate);
            Assert.Equal(0.6, PrivacyBudgetTracker.Instance.GetConsumedBudget("test-context"), 2);
        }

        [Fact]
        public void GivenOperation_WhenProcess_ShouldIncludePrivacyMetrics()
        {
            PrivacyBudgetTracker.Instance.SetTotalBudget("test-context", 1.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 0.2 },
                { "delta", 0.00001 },
                { "sensitivity", 1.0 },
                { "mechanism", "Laplace" },
                { "budgetContext", "test-context" }
            };

            var processor = new DifferentialPrivacyProcessor();
            var value = new FhirDecimal(50m);
            var node = ElementNode.FromElement(value.ToTypedElement());
            var result = processor.Process(node, null, settings);

            Assert.True(result.PrivacyMetrics.ContainsKey("epsilon-consumed"));
            Assert.Equal(0.2, result.PrivacyMetrics["epsilon-consumed"]);
            Assert.True(result.PrivacyMetrics.ContainsKey("mechanism"));
            Assert.True(result.PrivacyMetrics.ContainsKey("remaining-budget"));
        }

        [Fact]
        public void GivenEmptyNode_WhenProcess_ShouldReturnEmptyResult()
        {
            var settings = new Dictionary<string, object>
            {
                { "epsilon", 0.1 },
                { "sensitivity", 1.0 }
            };

            var processor = new DifferentialPrivacyProcessor();
            var emptyDecimal = new FhirDecimal();
            var node = ElementNode.FromElement(emptyDecimal.ToTypedElement());
            var result = processor.Process(node, null, settings);

            // Empty nodes should return result but not be marked as processed
            Assert.NotNull(result);
        }
    }
}
