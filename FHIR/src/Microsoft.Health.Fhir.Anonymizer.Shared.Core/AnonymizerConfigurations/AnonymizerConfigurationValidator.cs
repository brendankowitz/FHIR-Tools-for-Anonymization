using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    public class AnonymizerConfigurationValidator
    {
        private static readonly ILogger s_logger = AnonymizerLogging.CreateLogger<AnonymizerConfigurationValidator>();

        public void Validate(AnonymizerConfiguration config)
        {
            if (config == null)
            {
                throw new AnonymizerConfigurationException("Configuration is null.");
            }

            if (config.FhirVersion != null)
            {
                ValidateFhirVersion(config.FhirVersion);
            }

            ParameterConfigurationValidator.Validate(config.ParameterConfiguration);

            if (config.PathRules == null && config.TypeRules == null)
            {
                s_logger.LogWarning("No anonymization rules are defined (pathRules and typeRules are both null). " +
                    "All data will pass through without anonymization.");
                return;
            }

            var allRules = (config.PathRules ?? Enumerable.Empty<AnonymizerRule>())
                .Concat(config.TypeRules ?? Enumerable.Empty<AnonymizerRule>())
                .ToList();

            ValidateRuleList(allRules);
        }

        private static void ValidateFhirVersion(string fhirVersion)
        {
            var supportedVersions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "stu3",
                "r4",
                "r4b",
                "r5",
            };

            if (!supportedVersions.Contains(fhirVersion))
            {
                throw new AnonymizerConfigurationException(
                    $"Unsupported FHIR version '{fhirVersion}'. Supported versions are: {string.Join(", ", supportedVersions)}.");
            }
        }

        private static void ValidateRuleList(List<AnonymizerRule> rules)
        {
            foreach (var rule in rules)
            {
                if (string.IsNullOrWhiteSpace(rule.Path))
                {
                    throw new AnonymizerConfigurationException(
                        "An anonymization rule has a null or empty path. All rules must specify a non-empty path.");
                }

                if (string.IsNullOrWhiteSpace(rule.Method))
                {
                    throw new AnonymizerConfigurationException(
                        $"Rule for path '{rule.Path}' has a null or empty method. All rules must specify an anonymization method.");
                }
            }
        }
    }
}
