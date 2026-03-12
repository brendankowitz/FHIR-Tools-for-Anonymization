namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    using Newtonsoft.Json;

    /// <summary>
    /// Represents the configuration of a parameter in a rule.
    /// </summary>
    public class ParameterConfiguration
    {
        /// <summary>
        /// Gets or sets the type of the parameter.
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the value of the parameter.
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }
    }
}