namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    public class ParameterConfiguration
    {
        /// <summary>
        /// Gets or sets the name of the parameter.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value of the parameter.
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
}
