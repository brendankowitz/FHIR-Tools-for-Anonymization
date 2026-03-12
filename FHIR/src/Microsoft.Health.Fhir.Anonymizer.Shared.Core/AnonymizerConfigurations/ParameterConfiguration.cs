namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    using Newtonsoft.Json;

    /// <summary>
    /// The configuration of a parameter.
    /// </summary>
    public class ParameterConfiguration
    {
        /// <summary>
        /// Gets or sets the value of the parameter.
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the type of the parameter.
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
    }
}