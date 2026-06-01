using System;
using System.Linq;
using Hl7.FhirPath;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    /// <summary>
    /// Validates an <see cref="AnonymizerConfiguration"/> for structural correctness and security issues.
    /// </summary>
    public class AnonymizerConfigurationValidator
    {
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<AnonymizerConfigurationValidator>();

        /// <summary>
        /// Validates the given <paramref name="config"/>.
        /// </summary>
        /// <param name="config">The configuration to validate. Must not be null.</param>
        /// <exception cref="AnonymizerConfigurationException">
        /// Thrown when the configuration is structurally invalid (missing required fields,
        /// unsupported FHIR version, invalid FhirPath expressions, or invalid method settings).
        /// </exception>
        /// <remarks>
        /// A null <c>ParameterConfiguration</c> inside <paramref name="config"/> is explicitly
        /// permitted: it means no global parameters were configured and all parameter-level
        /// validation is intentionally skipped. This is NOT an oversight — missing configuration
        /// is safer than invalid configuration (Fail-Secure principle).
        /// </remarks>
        public void Validate(AnonymizerConfiguration config)
        {
            if (string.IsNullOrEmpty(config.FhirVersion))
            {
                _logger.LogWarning($"Version is not specified in configuration file.");
            }
            else if (!string.Equals(Constants.SupportedVersion, config.FhirVersion, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new AnonymizerConfigurationException($"Configuration of fhirVersion {config.FhirVersion} is not supported. Expected fhirVersion: {Constants.SupportedVersion}");
            }

            if (config.FhirPathRules == null)
            {
                throw new AnonymizerConfigurationException("The configuration is invalid, please specify any fhirPathRules");
            }

            FhirPathCompiler compiler = new FhirPathCompiler();
            var supportedMethods = Enum.GetNames(typeof(AnonymizerMethod)).ToHashSet(StringComparer.InvariantCultureIgnoreCase);
            foreach (var rule in config.FhirPathRules)
            {
                if (!rule.ContainsKey(Constants.PathKey) || !rule.ContainsKey(Constants.MethodKey))
                {
                    throw new AnonymizerConfigurationException("Missing path or method in Fhir path rule config.");
                }

                // Grammar check on FHIR path
                try
                {
                    compiler.Compile(rule[Constants.PathKey].ToString());
                }
                catch (Exception ex)
                {
                    throw new AnonymizerConfigurationException($"Invalid FHIR path {rule[Constants.PathKey]}", ex);
                }

                // Method validate
                string method = rule[Constants.MethodKey].ToString();
                if (!supportedMethods.Contains(method))
                {
                    _logger.LogWarning($"Anonymization method {method} is not a built-in method. Please make sure method {method} has been added as custom processor.");
                }

                // Should provide replacement value for substitute rule
                if (string.Equals(method, AnonymizerMethod.Substitute.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    SubstituteSetting.ValidateRuleSettings(rule);
                }

                if (string.Equals(method, AnonymizerMethod.Perturb.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    PerturbSetting.ValidateRuleSettings(rule);
                }
                if (string.Equals(method, AnonymizerMethod.Generalize.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    GeneralizeSetting.ValidateRuleSettings(rule);
                }
            }

            // null ParameterConfiguration is valid by design: it means no global parameters are
            // configured and all parameter-level validation (AES key size, placeholder detection,
            // date-shift offset range) is intentionally skipped. This is NOT an oversight.
            // See: Fail-Secure principle — missing configuration is safer than invalid configuration.
            ParameterConfigurationValidator.Validate(config.ParameterConfiguration);
        }
    }
}
