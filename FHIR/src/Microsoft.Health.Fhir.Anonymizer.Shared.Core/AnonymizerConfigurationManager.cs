using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Anonymizer.Core
{
    public sealed class AnonymizerConfigurationManager
    {
        private readonly AnonymizerConfigurationValidator _validator = new AnonymizerConfigurationValidator();
        private readonly AnonymizerConfiguration _configuration;

        public AnonymizationFhirPathRule[] FhirPathRules { get; private set; } = null;
        public AnonymizerConfiguration Configuration { get { return _configuration; } }

        public AnonymizerConfigurationManager(AnonymizerConfiguration configuration)
        {
            _validator.Validate(configuration);
            configuration.GenerateDefaultParametersIfNotConfigured();

            _configuration = configuration;

            FhirPathRules = _configuration.FhirPathRules.Select(entry => AnonymizationFhirPathRule.CreateAnonymizationFhirPathRule(entry)).ToArray();
        }

        /// <summary>
        /// Creates an <see cref="AnonymizerConfigurationManager"/> from a JSON string.
        /// </summary>
        /// <exception cref="AnonymizerConfigurationException">
        /// Thrown when the JSON is structurally invalid, e.g. when 'parameters' is not a JSON object.
        /// </exception>
        /// <exception cref="JsonException">Thrown when the JSON is malformed.</exception>
        public static AnonymizerConfigurationManager CreateFromSettingsInJson(string settingsInJson)
        {
            try
            {
                var token = JToken.Parse(settingsInJson, new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                });
                var configuration = token.ToObject<AnonymizerConfiguration>();

                // Explicitly parse the parameters block via ParameterConfigurationParser
                // rather than relying on implicit Newtonsoft.Json data-contract deserialization.
                //
                // WHY explicit parsing is required:
                //   1. Typed field validation — implicit deserialization silently accepts wrong JSON
                //      types (e.g. an integer where a string key is expected), producing values that
                //      cause cryptic runtime failures later.
                //   2. Duplicate-key detection — DuplicatePropertyNameHandling.Error is applied at
                //      the root level; ParameterConfigurationParser surfaces nested duplicates too.
                //   3. Fail-secure type guards — non-object values for object fields (e.g. a string
                //      for 'customSettings') are rejected with a clear AnonymizerConfigurationException
                //      rather than being silently coerced to null.
                var rawParametersToken = token["parameters"];
                if (rawParametersToken != null && rawParametersToken.Type != JTokenType.Null)
                {
                    if (rawParametersToken is not JObject parametersToken)
                        throw new AnonymizerConfigurationException("'parameters' must be a JSON object.");
                    configuration.ParameterConfiguration = ParameterConfigurationParser.Parse(parametersToken);
                }

                return new AnonymizerConfigurationManager(configuration);
            }
            catch (AnonymizerConfigurationException)
            {
                throw;
            }
            catch (JsonException innerException)
            {
                throw new AnonymizerConfigurationException($"Failed to parse configuration file", innerException);
            }
        }

        public static AnonymizerConfigurationManager CreateFromConfigurationFile(string configFilePath)
        {
            try
            {
                var content = File.ReadAllText(configFilePath);
                return CreateFromSettingsInJson(content);
            }
            catch (IOException innerException)
            {
                throw new AnonymizerConfigurationException($"Failed to read configuration file {configFilePath}", innerException);
            }
        }

        public ParameterConfiguration GetParameterConfiguration()
        {
            return _configuration.ParameterConfiguration;
        }

        public void SetDateShiftKeyPrefix(string prefix)
        {
            _configuration.ParameterConfiguration.DateShiftKeyPrefix = prefix;
        }
    }
}
