using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    [DataContract]
    public class AnonymizerConfiguration
    {
        [DataMember(Name = "fhirVersion")]
        public string FhirVersion { get; set; }

        [DataMember(Name = "processingErrors")]
        public ProcessingErrorsOption processingErrors { get; set; } = ProcessingErrorsOption.Raise;

        [DataMember(Name = "fhirPathRules")]
        public Dictionary<string, object>[] FhirPathRules { get; set; }

        /// <summary>
        /// Holds the parsed parameters block for this anonymizer configuration.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Marked <see cref="JsonIgnoreAttribute"/> so Newtonsoft.Json skips this property during
        /// automatic deserialization. Parsing is handled exclusively by
        /// <see cref="ParameterConfigurationParser"/>, which provides typed field validation,
        /// duplicate-key detection, and fail-secure type guards that implicit deserialization cannot.
        /// </para>
        /// <para>
        /// No <c>[DataMember]</c> attribute is present because DataContractSerializer is not used
        /// on this class; omitting it prevents ambiguity between the two serialization systems.
        /// </para>
        /// </remarks>
        // Parsing is handled explicitly by ParameterConfigurationParser;
        // DataContractSerializer is not used on this class.
        [JsonIgnore]
        public ParameterConfiguration ParameterConfiguration { get; set; }

        // Static default crypto hash key shared across all engine instances.
        private static readonly Lazy<string> s_defaultCryptoKey = new Lazy<string>(() => Guid.NewGuid().ToString("N"));

        public void GenerateDefaultParametersIfNotConfigured()
        {
            if (ParameterConfiguration == null)
            {
                ParameterConfiguration = new ParameterConfiguration
                {
                    DateShiftKey  = Guid.NewGuid().ToString("N"),
                    CryptoHashKey = s_defaultCryptoKey.Value,
                    EncryptKey    = s_defaultCryptoKey.Value
                };
                return;
            }

            if (string.IsNullOrEmpty(ParameterConfiguration.DateShiftKey))
                ParameterConfiguration.DateShiftKey = Guid.NewGuid().ToString("N");

            if (string.IsNullOrEmpty(ParameterConfiguration.CryptoHashKey))
                ParameterConfiguration.CryptoHashKey = s_defaultCryptoKey.Value;

            if (string.IsNullOrEmpty(ParameterConfiguration.EncryptKey))
                ParameterConfiguration.EncryptKey = s_defaultCryptoKey.Value;
        }
    }
}
