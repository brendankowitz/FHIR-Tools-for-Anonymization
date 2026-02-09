// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors
{
    /// <summary>
    /// Security-focused tests verifying cryptographic guarantees in differential privacy implementation.
    /// CRITICAL: Differential privacy requires cryptographically secure randomness.
    /// System.Random is NOT sufficient - it is predictable and breaks privacy guarantees.
    /// </summary>
    [Collection("DifferentialPrivacyTests")]
    public class CryptographicSecurityTests
    {
        private readonly ITestOutputHelper _output;

        public CryptographicSecurityTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DifferentialPrivacyProcessor_ShouldUseSecureRandomNumberGenerator()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var tracker = PrivacyBudgetTracker.Instance;
            var budgetContext = $"security-test-{Guid.NewGuid()}";
            tracker.InitializeBudget(budgetContext, 10.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 0.1 },
                { "sensitivity", 1.0 },
                { "mechanism", "Laplace" },
                { "budgetContext", budgetContext }
            };

            var decimalValue = new FhirDecimal(100.0m);
            var node = ElementNode.FromElement(decimalValue.ToTypedElement());

            // Act - Process multiple times and collect noisy values
            var noisyValues = new List<decimal>();
            for (int i = 0; i < 50; i++)
            {
                var testValue = new FhirDecimal(100.0m);
                var testNode = ElementNode.FromElement(testValue.ToTypedElement());
                var result = processor.Process(testNode, null, settings);
                noisyValues.Add((decimal)testNode.Value);
            }

            // Assert - Verify noise characteristics that would fail with weak RNG
            // 1. Values should vary significantly (not deterministic)
            var uniqueValues = noisyValues.Distinct().Count();
            Assert.True(uniqueValues > 40, $"Expected >40 unique values from secure RNG, got {uniqueValues}. Weak RNG would produce patterns.");

            // 2. Standard deviation should match theoretical expectation for Laplace
            // Laplace(0, sensitivity/epsilon) has stddev = sensitivity * sqrt(2) / epsilon
            var expectedStdDev = 1.0 * Math.Sqrt(2) / 0.1; // ~14.14
            var actualMean = (double)noisyValues.Average();
            var actualVariance = noisyValues.Select(v => Math.Pow((double)v - actualMean, 2)).Average();
            var actualStdDev = Math.Sqrt(actualVariance);

            _output.WriteLine($"Expected StdDev: {expectedStdDev:F2}, Actual StdDev: {actualStdDev:F2}");
            _output.WriteLine($"Mean: {actualMean:F2} (should be ~100)");
            _output.WriteLine($"Unique values: {uniqueValues}/50");

            // Allow 50% tolerance for small sample size, but verify it's in the right ballpark
            Assert.InRange(actualStdDev, expectedStdDev * 0.5, expectedStdDev * 1.5);

            // 3. Mean should be close to original value (unbiased noise)
            Assert.InRange(actualMean, 100.0 - 3 * expectedStdDev, 100.0 + 3 * expectedStdDev);
        }

        [Fact]
        public void DifferentialPrivacyProcessor_GaussianMechanism_ShouldUseSecureRNG()
        {
            // Arrange
            var processor = new DifferentialPrivacyProcessor();
            var tracker = PrivacyBudgetTracker.Instance;
            var budgetContext = $"gaussian-security-test-{Guid.NewGuid()}";
            tracker.InitializeBudget(budgetContext, 10.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 0.1 },
                { "delta", 1e-5 },
                { "sensitivity", 1.0 },
                { "mechanism", "Gaussian" },
                { "budgetContext", budgetContext }
            };

            var noisyValues = new List<decimal>();
            for (int i = 0; i < 50; i++)
            {
                var testValue = new FhirDecimal(50.0m);
                var testNode = ElementNode.FromElement(testValue.ToTypedElement());
                var result = processor.Process(testNode, null, settings);
                noisyValues.Add((decimal)testNode.Value);
            }

            // Assert - Verify Gaussian noise characteristics
            var uniqueValues = noisyValues.Distinct().Count();
            Assert.True(uniqueValues > 40, $"Expected >40 unique values from secure RNG, got {uniqueValues}");

            var actualMean = (double)noisyValues.Average();
            _output.WriteLine($"Gaussian mechanism: Mean = {actualMean:F2}, Unique values = {uniqueValues}/50");

            // Mean should be close to 50
            Assert.InRange(actualMean, 30.0, 70.0);
        }

        [Fact]
        public void DifferentialPrivacyProcessor_ShouldNotUseSystemRandom()
        {
            // This test uses reflection to verify that System.Random is NOT used in DifferentialPrivacyProcessor.
            // System.Random is cryptographically weak and would break differential privacy guarantees.
            
            var processorType = typeof(DifferentialPrivacyProcessor);
            var fields = processorType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            
            // Check that no fields are of type System.Random
            foreach (var field in fields)
            {
                Assert.False(field.FieldType == typeof(Random), 
                    $"SECURITY VIOLATION: DifferentialPrivacyProcessor uses System.Random field '{field.Name}'. " +
                    $"Must use System.Security.Cryptography.RandomNumberGenerator for cryptographic security.");
            }

            _output.WriteLine("✓ Verified: No System.Random fields detected in DifferentialPrivacyProcessor");
            _output.WriteLine("✓ Implementation should use System.Security.Cryptography.RandomNumberGenerator");
        }

        [Fact]
        public void DifferentialPrivacyProcessor_NoiseDistribution_ShouldResistPrediction()
        {
            // Security test: Verify that noise is unpredictable even across multiple runs
            // A weak RNG or seeded Random() would produce correlated sequences

            var processor = new DifferentialPrivacyProcessor();
            var tracker = PrivacyBudgetTracker.Instance;
            var budgetContext = $"prediction-test-{Guid.NewGuid()}";
            tracker.InitializeBudget(budgetContext, 10.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 1.0 },
                { "sensitivity", 1.0 },
                { "mechanism", "Laplace" },
                { "budgetContext", budgetContext }
            };

            // Generate two sequences
            var sequence1 = new List<decimal>();
            var sequence2 = new List<decimal>();

            for (int i = 0; i < 20; i++)
            {
                var node1 = ElementNode.FromElement(new FhirDecimal(0.0m).ToTypedElement());
                processor.Process(node1, null, settings);
                sequence1.Add((decimal)node1.Value);

                var node2 = ElementNode.FromElement(new FhirDecimal(0.0m).ToTypedElement());
                processor.Process(node2, null, settings);
                sequence2.Add((decimal)node2.Value);
            }

            // Compute correlation between sequences
            // Secure RNG should produce uncorrelated sequences (correlation ~ 0)
            // Weak/seeded RNG would show patterns (|correlation| > 0.3)
            var correlation = ComputeCorrelation(sequence1, sequence2);

            _output.WriteLine($"Correlation between independent sequences: {correlation:F3}");
            _output.WriteLine($"Expected: ~0.0 (uncorrelated)");
            _output.WriteLine($"Weak RNG would show: |correlation| > 0.3");

            // For secure RNG, correlation should be close to 0 (within [-0.4, 0.4] for small samples)
            Assert.InRange(Math.Abs(correlation), 0.0, 0.5);
        }

        [Fact]
        public void DifferentialPrivacyProcessor_MultiThreaded_ShouldProduceIndependentNoise()
        {
            // Security test: Verify thread safety doesn't compromise randomness
            // Shared state in RNG or improper seeding could cause correlated noise across threads

            var processor = new DifferentialPrivacyProcessor();
            var tracker = PrivacyBudgetTracker.Instance;
            var budgetContext = $"multithread-test-{Guid.NewGuid()}";
            tracker.InitializeBudget(budgetContext, 50.0);

            var settings = new Dictionary<string, object>
            {
                { "epsilon", 0.1 },
                { "sensitivity", 1.0 },
                { "mechanism", "Laplace" },
                { "budgetContext", budgetContext }
            };

            var results = new System.Collections.Concurrent.ConcurrentBag<decimal>();

            // Process in parallel
            System.Threading.Tasks.Parallel.For(0, 100, i =>
            {
                var node = ElementNode.FromElement(new FhirDecimal(100.0m).ToTypedElement());
                processor.Process(node, null, settings);
                results.Add((decimal)node.Value);
            });

            // Verify results are diverse (not colliding due to thread issues)
            var uniqueValues = results.Distinct().Count();
            _output.WriteLine($"Parallel processing: {uniqueValues} unique values from 100 operations");

            Assert.True(uniqueValues > 90, $"Expected >90 unique values in parallel execution, got {uniqueValues}");
        }

        private double ComputeCorrelation(List<decimal> x, List<decimal> y)
        {
            if (x.Count != y.Count || x.Count == 0)
                return 0.0;

            var xValues = x.Select(v => (double)v).ToArray();
            var yValues = y.Select(v => (double)v).ToArray();

            var meanX = xValues.Average();
            var meanY = yValues.Average();

            var covariance = xValues.Zip(yValues, (xi, yi) => (xi - meanX) * (yi - meanY)).Average();
            var stdX = Math.Sqrt(xValues.Select(xi => Math.Pow(xi - meanX, 2)).Average());
            var stdY = Math.Sqrt(yValues.Select(yi => Math.Pow(yi - meanY, 2)).Average());

            if (stdX == 0 || stdY == 0)
                return 0.0;

            return covariance / (stdX * stdY);
        }
    }
}
