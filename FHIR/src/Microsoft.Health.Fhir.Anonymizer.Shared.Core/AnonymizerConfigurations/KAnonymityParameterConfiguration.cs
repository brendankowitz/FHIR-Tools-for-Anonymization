using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    /// <summary>
    /// Configuration parameters for k-anonymity processing.
    /// </summary>
    [DataContract]
    public class KAnonymityParameterConfiguration
    {
        private static readonly ILogger s_logger = AnonymizerLogging.CreateLogger<KAnonymityParameterConfiguration>();

        /// <summary>
        /// Minimum group size for k-anonymity (default: 5).
        /// Each combination of quasi-identifiers must appear in at least k records.
        /// Higher values provide stronger privacy but may require more aggressive generalization.
        /// </summary>
        [DataMember(Name = "kValue")]
        public int KValue { get; set; } = 5;

        /// <summary>
        /// List of FHIR paths to quasi-identifiers.
        /// Quasi-identifiers are attributes that together could identify individuals.
        /// Example: ["Patient.birthDate", "Patient.address.postalCode", "Patient.gender"]
        /// </summary>
        [DataMember(Name = "quasiIdentifiers")]
        public List<string> QuasiIdentifiers { get; set; }

        /// <summary>
        /// Generalization hierarchies for quasi-identifiers (optional).
        /// Maps FHIR path to generalization strategy configuration.
        /// Defines how values should be generalized to achieve k-anonymity.
        /// </summary>
        [DataMember(Name = "generalizationHierarchies")]
        public Dictionary<string, object> GeneralizationHierarchies { get; set; }

        /// <summary>
        /// Suppression threshold (0.0-1.0). Records that cannot be generalized to meet
        /// k-anonymity within this fraction of the dataset will be suppressed (removed).
        /// Default: 0.3 (30%). High suppression rates indicate data utility loss.
        /// </summary>
        [DataMember(Name = "suppressionThreshold")]
        public double SuppressionThreshold { get; set; } = 0.3;

        /// <summary>
        /// Validates the k-anonymity configuration parameters.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
        public void Validate()
        {
            if (KValue < 2)
            {
                throw new ArgumentException(
                    $"K-anonymity k-value must be at least 2 (provided: {KValue}). " +
                    "k=1 provides no privacy protection.");
            }

            if (KValue == 2)
            {
                s_logger.LogWarning(
                    "K-anonymity k-value is 2 (minimal). Consider k >= 5 for better privacy protection " +
                    "(recommended by HIPAA Safe Harbor guidance).");
            }

            if (SuppressionThreshold < 0 || SuppressionThreshold > 1)
            {
                throw new ArgumentException(
                    "K-anonymity suppression threshold must be between 0 and 1 (represents percentage)");
            }
        }
    }
}
