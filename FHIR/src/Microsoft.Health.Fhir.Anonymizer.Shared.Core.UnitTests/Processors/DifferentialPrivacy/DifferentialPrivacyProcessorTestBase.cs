using System;
using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors.DifferentialPrivacy
{
    /// <summary>
    /// Base class for DifferentialPrivacyProcessor tests providing common helper methods and test utilities.
    /// </summary>
    public class DifferentialPrivacyProcessorTestBase
    {
        /// <summary>
        /// Creates a DifferentialPrivacyProcessor instance for testing.
        /// </summary>
        /// <returns>A new DifferentialPrivacyProcessor instance.</returns>
        protected DifferentialPrivacyProcessor CreateProcessor()
        {
            return new DifferentialPrivacyProcessor();
        }

        /// <summary>
        /// Creates settings dictionary with differential privacy configuration.
        /// </summary>
        /// <param name="epsilon">Privacy budget parameter (default: 1.0).</param>
        /// <param name="sensitivity">Sensitivity parameter (default: 1.0).</param>
        /// <param name="mechanism">Noise mechanism (default: "laplace").</param>
        /// <param name="seed">Random seed for reproducible tests (optional).</param>
        /// <returns>Dictionary of settings for differential privacy.</returns>
        protected Dictionary<string, object> CreateSettings(double epsilon = 1.0, double sensitivity = 1.0, string mechanism = "laplace", int? seed = null)
        {
            var settings = new Dictionary<string, object>
            {
                ["epsilon"] = epsilon,
                ["sensitivity"] = sensitivity,
                ["mechanism"] = mechanism
            };

            if (seed.HasValue)
            {
                settings["seed"] = seed.Value;
            }

            return settings;
        }

        /// <summary>
        /// Creates an ElementNode for testing with the specified value.
        /// </summary>
        /// <param name="value">The value to set on the node.</param>
        /// <returns>A new ElementNode with the specified value.</returns>
        protected ElementNode CreateNode(object value)
        {
            return new ElementNode
            {
                Value = value
            };
        }

        /// <summary>
        /// Asserts that a value is within the expected range.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="min">Minimum expected value.</param>
        /// <param name="max">Maximum expected value.</param>
        protected void AssertInRange(double value, double min, double max)
        {
            Assert.True(value >= min && value <= max, $"Expected value to be in range [{min}, {max}], but was {value}");
        }
    }
}
