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
        /// Gets or sets the name of the parameter.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of the parameter.
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the list of values for the parameter.
        /// </summary>
        [JsonProperty(PropertyName = "values")]
        public List<object> Values { get; set; } = new List<object>();
    }
}
