using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    public class ParameterConfiguration
    {
        /// <summary>
        /// Gets or sets the parameter type.
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameter value.
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the parameter fields for object value.
        /// </summary>
        [JsonProperty(PropertyName = "fields")]
        public Dictionary<string, object> Fields { get; set; } = new Dictionary<string, object>();
    }
}