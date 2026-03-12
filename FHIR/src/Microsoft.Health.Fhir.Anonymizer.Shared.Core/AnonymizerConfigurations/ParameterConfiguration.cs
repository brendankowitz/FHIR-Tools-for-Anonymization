using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    /// <summary>
    /// Represents a parameter configuration.
    /// </summary>
    public class ParameterConfiguration
    {
        /// <summary>
        /// Gets or sets the type of the parameter.
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value of the parameter.
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public object? Value { get; set; } = null;

        /// <summary>
        /// Gets or sets the settings of the parameter.
        /// </summary>
        [JsonProperty(PropertyName = "settings")]
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
    }
}