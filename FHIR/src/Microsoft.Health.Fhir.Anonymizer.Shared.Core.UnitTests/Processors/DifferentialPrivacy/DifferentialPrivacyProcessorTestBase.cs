using System;
using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors.DifferentialPrivacy
{
    /// <summary>
    /// Base class for DifferentialPrivacyProcessor tests.
    /// Provides shared test fixtures, helper methods, and common setup/teardown logic.
    /// Organization: Tests are split into focused classes by functionality:
    /// - ConfigurationTests: epsilon, delta, sensitivity parameters
    /// - NoiseTests: Laplace, Gaussian mechanisms
    /// - PrivacyBudgetTests: budget tracking, depletion
    /// - ValidationTests: input validation, boundary conditions
    /// </summary>
    public abstract class DifferentialPrivacyProcessorTestBase
    {
        protected const double DefaultEpsilon = 1.0;
        protected const double DefaultDelta = 1e-5;
        protected const double DefaultSensitivity = 1.0;
        protected const double Tolerance = 1e-10;

        /// <summary>
        /// Creates a test configuration dictionary with default parameters.
        /// </summary>
        protected Dictionary<string, object> CreateDefaultConfig()
        {
            return new Dictionary<string, object>
            {
                ["epsilon"] = DefaultEpsilon,
                ["delta"] = DefaultDelta,
                ["sensitivity"] = DefaultSensitivity
            };
        }

        /// <summary>
        /// Creates a test configuration with custom epsilon value.
        /// </summary>
        protected Dictionary<string, object> CreateConfigWithEpsilon(double epsilon)
        {
            var config = CreateDefaultConfig();
            config["epsilon"] = epsilon;
            return config;
        }

        /// <summary>
        /// Creates a test configuration with custom delta value.
        /// </summary>
        protected Dictionary<string, object> CreateConfigWithDelta(double delta)
        {
            var config = CreateDefaultConfig();
            config["delta"] = delta;
            return config;
        }

        /// <summary>
        /// Creates a test configuration with custom sensitivity value.
        /// </summary>
        protected Dictionary<string, object> CreateConfigWithSensitivity(double sensitivity)
        {
            var config = CreateDefaultConfig();
            config["sensitivity"] = sensitivity;
            return config;
        }

        /// <summary>
        /// Creates a test configuration with noise mechanism specified.
        /// </summary>
        protected Dictionary<string, object> CreateConfigWithMechanism(string mechanism)
        {
            var config = CreateDefaultConfig();
            config["mechanism"] = mechanism;
            return config;
        }

        /// <summary>
        /// Creates a test configuration with privacy budget.
        /// </summary>
        protected Dictionary<string, object> CreateConfigWithBudget(double budget)
        {
            var config = CreateDefaultConfig();
            config["privacyBudget"] = budget;
            return config;
        }

        /// <summary>
        /// Asserts that two doubles are approximately equal within tolerance.
        /// </summary>
        protected void AssertApproximatelyEqual(double expected, double actual, double tolerance = Tolerance)
        {
            Assert.True(Math.Abs(expected - actual) < tolerance,
                $"Expected {expected} but got {actual} (tolerance: {tolerance})");
        }

        /// <summary>
        /// Asserts that a value is within a specified range.
        /// </summary>
        protected void AssertInRange(double value, double min, double max)
        {
            Assert.True(value >= min && value <= max,
                $"Expected value to be in range [{min}, {max}] but got {value}");
        }

        /// <summary>
        /// Asserts that noise was added (value changed from original).
        /// </summary>
        protected void AssertNoiseAdded(double original, double noised)
        {
            Assert.NotEqual(original, noised);
        }

        /// <summary>
        /// Creates test data array with specified values.
        /// </summary>
        protected double[] CreateTestData(params double[] values)
        {
            return values;
        }

        /// <summary>
        /// Creates test data array with uniform values.
        /// </summary>
        protected double[] CreateUniformTestData(int count, double value)
        {
            var data = new double[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = value;
            }
            return data;
        }

        /// <summary>
        /// Creates sequential test data (0, 1, 2, ...).
        /// </summary>
        protected double[] CreateSequentialTestData(int count)
        {
            var data = new double[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = i;
            }
            return data;
        }
    }
}
