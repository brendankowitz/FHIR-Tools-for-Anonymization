using System;
using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors.DifferentialPrivacy
{
    public class DifferentialPrivacyProcessorPrivacyBudgetTests : DifferentialPrivacyProcessorTestBase, IDisposable
    {
        private const int FixedSeed = 98765;

        public DifferentialPrivacyProcessorPrivacyBudgetTests()
        {
            // Reset the singleton tracker before each test
            PrivacyBudgetTracker.Instance.Reset();
        }

        public void Dispose()
        {
            // Clean up after each test
            PrivacyBudgetTracker.Instance.Reset();
        }

        [Fact]
        public void Process_TracksEpsilonConsumption()
        {
            // Arrange
            var processor = CreateProcessor();
            var node = CreateNode(100.0);
            var settings = CreateSettings(epsilon: 1.0, sensitivity: 1.0, mechanism: "laplace", seed: FixedSeed);
            var initialBudget = PrivacyBudgetTracker.Instance.GetRemainingBudget();

            // Act
            processor.Process(node, settings);

            // Assert
            var remainingBudget = PrivacyBudgetTracker.Instance.GetRemainingBudget();
            Assert.True(remainingBudget < initialBudget, "Privacy budget should be consumed");
        }

        [Fact]
        public void Process_MultipleCalls_AccumulatesBudgetConsumption()
        {
            // Arrange
            var processor = CreateProcessor();
            var epsilon = 0.5;
            var settings = CreateSettings(epsilon: epsilon, sensitivity: 1.0, mechanism: "laplace", seed: FixedSeed);
            var initialBudget = PrivacyBudgetTracker.Instance.GetRemainingBudget();

            // Act
            processor.Process(CreateNode(100.0), settings);
            var budgetAfterFirst = PrivacyBudgetTracker.Instance.GetRemainingBudget();
            
            processor.Process(CreateNode(200.0), settings);
            var budgetAfterSecond = PrivacyBudgetTracker.Instance.GetRemainingBudget();

            // Assert
            Assert.True(budgetAfterFirst < initialBudget, "Budget consumed after first call");
            Assert.True(budgetAfterSecond < budgetAfterFirst, "Budget consumed after second call");
            Assert.Equal(initialBudget - (2 * epsilon), budgetAfterSecond, 6);
        }

        [Fact]
        public void Process_WithDifferentEpsilonValues_TracksSeparately()
        {
            // Arrange
            var processor = CreateProcessor();
            var epsilon1 = 0.5;
            var epsilon2 = 1.0;
            var settings1 = CreateSettings(epsilon: epsilon1, sensitivity: 1.0, mechanism: "laplace", seed: FixedSeed);
            var settings2 = CreateSettings(epsilon: epsilon2, sensitivity: 1.0, mechanism: "laplace", seed: FixedSeed);
            var initialBudget = PrivacyBudgetTracker.Instance.GetRemainingBudget();

            // Act
            processor.Process(CreateNode(100.0), settings1);
            processor.Process(CreateNode(200.0), settings2);

            // Assert
            var remainingBudget = PrivacyBudgetTracker.Instance.GetRemainingBudget();
            Assert.Equal(initialBudget - epsilon1 - epsilon2, remainingBudget, 6);
        }

        [Fact]
        public void GetRemainingBudget_ReturnsCorrectValue()
        {
            // Arrange
            var processor = CreateProcessor();
            var epsilon = 2.0;
            var settings = CreateSettings(epsilon: epsilon, sensitivity: 1.0, mechanism: "laplace", seed: FixedSeed);
            var initialBudget = PrivacyBudgetTracker.Instance.GetRemainingBudget();

            // Act
            processor.Process(CreateNode(100.0), settings);

            // Assert
            var remainingBudget = PrivacyBudgetTracker.Instance.GetRemainingBudget();
            Assert.Equal(initialBudget - epsilon, remainingBudget, 6);
        }

        [Fact]
        public void GetTotalConsumed_ReturnsAccumulatedConsumption()
        {
            // Arrange
            var processor = CreateProcessor();
            var epsilon = 0.5;
            var settings = CreateSettings(epsilon: epsilon, sensitivity: 1.0, mechanism: "laplace", seed: FixedSeed);

            // Act
            processor.Process(CreateNode(100.0), settings);
            processor.Process(CreateNode(200.0), settings);
            processor.Process(CreateNode(300.0), settings);

            // Assert
            var totalConsumed = PrivacyBudgetTracker.Instance.GetTotalConsumed();
            Assert.Equal(3 * epsilon, totalConsumed, 6);
        }

        [Fact]
        public void Reset_RestoresBudget()
        {
            // Arrange
            var processor = CreateProcessor();
            var settings = CreateSettings(epsilon: 1.0, sensitivity: 1.0, mechanism: "laplace", seed: FixedSeed);
            var initialBudget = PrivacyBudgetTracker.Instance.GetRemainingBudget();
            
            processor.Process(CreateNode(100.0), settings);
            Assert.True(PrivacyBudgetTracker.Instance.GetRemainingBudget() < initialBudget, "Budget should be consumed");

            // Act
            PrivacyBudgetTracker.Instance.Reset();

            // Assert
            var resetBudget = PrivacyBudgetTracker.Instance.GetRemainingBudget();
            Assert.Equal(initialBudget, resetBudget, 6);
        }

        [Fact]
        public void Singleton_ReturnsSameInstance()
        {
            // Arrange & Act
            var instance1 = PrivacyBudgetTracker.Instance;
            var instance2 = PrivacyBudgetTracker.Instance;

            // Assert
            Assert.Same(instance1, instance2);
        }
    }
}
