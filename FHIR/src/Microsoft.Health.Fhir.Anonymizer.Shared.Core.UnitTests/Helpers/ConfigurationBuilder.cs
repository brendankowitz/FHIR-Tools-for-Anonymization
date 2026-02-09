// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Helpers
{
    /// <summary>
    /// Fluent builder for AnonymizerConfiguration to simplify test setup
    /// Provides type-safe construction of configurations with sensible defaults
    /// </summary>
    public class ConfigurationBuilder
    {
        private readonly Dictionary<string, AnonymizerRule> _pathRules = new Dictionary<string, AnonymizerRule>();
        private readonly Dictionary<string, string> _fhirPathRules = new Dictionary<string, string>();
        private readonly Dictionary<string, AnonymizerRule> _typeRules = new Dictionary<string, AnonymizerRule>();

        /// <summary>
        /// Creates a new configuration builder
        /// </summary>
        public static ConfigurationBuilder Create() => new ConfigurationBuilder();

        /// <summary>
        /// Adds a path-based anonymization rule
        /// </summary>
        public ConfigurationBuilder WithPathRule(string path, AnonymizerMethod method, ParameterConfiguration parameters = null)
        {
            _pathRules[path] = new AnonymizerRule
            {
                Path = path,
                Method = method,
                Cases = new List<AnonymizerRule>(),
                Parameters = parameters ?? new ParameterConfiguration()
            };
            return this;
        }

        /// <summary>
        /// Adds a differential privacy rule with standard parameters
        /// </summary>
        public ConfigurationBuilder WithDifferentialPrivacy(
            string path,
            double epsilon,
            double sensitivity = 1.0,
            string mechanism = "Laplace",
            string budgetContext = null,
            double? delta = null)
        {
            var parameters = new ParameterConfiguration
            {
                { RuleKeys.Epsilon, epsilon.ToString() },
                { RuleKeys.Sensitivity, sensitivity.ToString() },
                { RuleKeys.Mechanism, mechanism },
                { RuleKeys.BudgetContext, budgetContext ?? $"test-context-{Guid.NewGuid()}" }
            };

            if (delta.HasValue)
            {
                parameters.Add(RuleKeys.Delta, delta.Value.ToString());
            }

            return WithPathRule(path, AnonymizerMethod.DifferentialPrivacy, parameters);
        }

        /// <summary>
        /// Adds a k-anonymity rule with quasi-identifiers
        /// </summary>
        public ConfigurationBuilder WithKAnonymity(int k, params string[] quasiIdentifiers)
        {
            var parameters = new ParameterConfiguration
            {
                { RuleKeys.KValue, k.ToString() },
                { RuleKeys.QuasiIdentifiers, string.Join(",", quasiIdentifiers) }
            };

            return WithPathRule("*", AnonymizerMethod.KAnonymity, parameters);
        }

        /// <summary>
        /// Adds a redaction rule
        /// </summary>
        public ConfigurationBuilder WithRedaction(string path)
        {
            return WithPathRule(path, AnonymizerMethod.Redact);
        }

        /// <summary>
        /// Adds a date shift rule
        /// </summary>
        public ConfigurationBuilder WithDateShift(string path, int dateShiftRange, string dateShiftKey, string dateShiftScope = "resource")
        {
            var parameters = new ParameterConfiguration
            {
                { RuleKeys.DateShiftRange, dateShiftRange.ToString() },
                { RuleKeys.DateShiftKey, dateShiftKey },
                { RuleKeys.DateShiftScope, dateShiftScope }
            };

            return WithPathRule(path, AnonymizerMethod.DateShift, parameters);
        }

        /// <summary>
        /// Adds a cryptographic hash rule
        /// </summary>
        public ConfigurationBuilder WithCryptoHash(string path, string cryptoHashKey)
        {
            var parameters = new ParameterConfiguration
            {
                { RuleKeys.CryptoHashKey, cryptoHashKey }
            };

            return WithPathRule(path, AnonymizerMethod.CryptoHash, parameters);
        }

        /// <summary>
        /// Adds an encryption rule
        /// </summary>
        public ConfigurationBuilder WithEncrypt(string path, string encryptKey)
        {
            var parameters = new ParameterConfiguration
            {
                { RuleKeys.EncryptKey, encryptKey }
            };

            return WithPathRule(path, AnonymizerMethod.Encrypt, parameters);
        }

        /// <summary>
        /// Adds a substitution rule
        /// </summary>
        public ConfigurationBuilder WithSubstitute(string path, string replaceWith)
        {
            var parameters = new ParameterConfiguration
            {
                { RuleKeys.ReplaceWith, replaceWith }
            };

            return WithPathRule(path, AnonymizerMethod.Substitute, parameters);
        }

        /// <summary>
        /// Adds a FHIRPath expression rule
        /// </summary>
        public ConfigurationBuilder WithFhirPathRule(string fhirPath, string method)
        {
            _fhirPathRules[fhirPath] = method;
            return this;
        }

        /// <summary>
        /// Adds a type-based anonymization rule
        /// </summary>
        public ConfigurationBuilder WithTypeRule(string typeName, AnonymizerMethod method, ParameterConfiguration parameters = null)
        {
            _typeRules[typeName] = new AnonymizerRule
            {
                Path = typeName,
                Method = method,
                Cases = new List<AnonymizerRule>(),
                Parameters = parameters ?? new ParameterConfiguration()
            };
            return this;
        }

        /// <summary>
        /// Builds the final AnonymizerConfiguration
        /// </summary>
        public AnonymizerConfiguration Build()
        {
            return new AnonymizerConfiguration
            {
                PathRules = _pathRules,
                FhirPathRules = _fhirPathRules,
                TypeRules = _typeRules
            };
        }

        /// <summary>
        /// Initializes a privacy budget for differential privacy operations
        /// </summary>
        public ConfigurationBuilder InitializeBudget(string context, double totalBudget)
        {
            PrivacyBudgetTracker.Instance.InitializeBudget(context, totalBudget);
            return this;
        }

        /// <summary>
        /// Creates a unique budget context name following naming conventions
        /// Format: dataset-id:operation-id:timestamp
        /// </summary>
        public static string CreateBudgetContext(string datasetId, string operationId)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            return $"{datasetId}:{operationId}:{timestamp}";
        }
    }
}
