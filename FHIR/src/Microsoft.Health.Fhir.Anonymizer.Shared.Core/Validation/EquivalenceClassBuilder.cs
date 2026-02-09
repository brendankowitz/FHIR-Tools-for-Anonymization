using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Validation
{
    /// <summary>
    /// Builds equivalence classes from FHIR resources based on quasi-identifiers
    /// </summary>
    public class EquivalenceClassBuilder
    {
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<EquivalenceClassBuilder>();

        /// <summary>
        /// Builds equivalence classes from a collection of FHIR resources
        /// </summary>
        /// <param name="resources">Collection of FHIR resources (as JSON strings)</param>
        /// <param name="quasiIdentifierPaths">FHIR paths to quasi-identifier fields (e.g., "Patient.birthDate", "Patient.gender", "Patient.address.postalCode")</param>
        /// <returns>List of equivalence classes grouped by identical quasi-identifier values</returns>
        public List<List<Dictionary<string, object>>> BuildFromResources(
            IEnumerable<string> resources,
            IEnumerable<string> quasiIdentifierPaths)
        {
            if (resources == null || !resources.Any())
            {
                return new List<List<Dictionary<string, object>>>();
            }

            if (quasiIdentifierPaths == null || !quasiIdentifierPaths.Any())
            {
                throw new ArgumentException("At least one quasi-identifier path must be specified.", nameof(quasiIdentifierPaths));
            }

            // Extract quasi-identifiers from each resource
            var resourceQuasiIdentifiers = new List<Dictionary<string, object>>();
            int skippedCount = 0;
            foreach (var resourceJson in resources)
            {
                try
                {
                    var quasiIdentifiers = ExtractQuasiIdentifiers(resourceJson, quasiIdentifierPaths);
                    resourceQuasiIdentifiers.Add(quasiIdentifiers);
                }
                catch (Exception ex)
                {
                    // Skip resources that fail to parse and log the failure
                    skippedCount++;
                    _logger.LogWarning(ex, $"Failed to extract quasi-identifiers from resource. Skipping resource. Error: {ex.Message}");
                    continue;
                }
            }

            if (skippedCount > 0)
            {
                _logger.LogWarning($"Skipped {skippedCount} resource(s) due to parsing failures during equivalence class building.");
            }

            // Group resources by identical quasi-identifier values
            var equivalenceClasses = GroupByQuasiIdentifiers(resourceQuasiIdentifiers);

            return equivalenceClasses;
        }

        /// <summary>
        /// Extracts quasi-identifier values from a single FHIR resource
        /// </summary>
        private Dictionary<string, object> ExtractQuasiIdentifiers(string resourceJson, IEnumerable<string> quasiIdentifierPaths)
        {
            var quasiIdentifiers = new Dictionary<string, object>();
            var jObject = JObject.Parse(resourceJson);

            foreach (var path in quasiIdentifierPaths)
            {
                try
                {
                    var value = ExtractValueByPath(jObject, path);
                    var key = GetSimplifiedKey(path);
                    quasiIdentifiers[key] = value ?? "[REDACTED]";
                }
                catch (Exception ex)
                {
                    // If path extraction fails, use redacted value and log the failure
                    var key = GetSimplifiedKey(path);
                    quasiIdentifiers[key] = "[REDACTED]";
                    _logger.LogDebug(ex, $"Failed to extract value for path '{path}'. Using [REDACTED]. Error: {ex.Message}");
                }
            }

            return quasiIdentifiers;
        }

        /// <summary>
        /// Extracts value from JObject using simplified FHIR path notation
        /// </summary>
        private object ExtractValueByPath(JObject resource, string path)
        {
            // Handle simple paths like "Patient.gender", "Patient.birthDate", "Patient.address.postalCode"
            var parts = path.Split('.');
            JToken current = resource;

            // Skip resource type if it's the first part
            var startIndex = (parts.Length > 0 && parts[0] == resource["resourceType"]?.ToString()) ? 1 : 0;

            for (int i = startIndex; i < parts.Length; i++)
            {
                if (current == null)
                {
                    return null;
                }

                // Handle array notation
                if (current is JArray array)
                {
                    // For arrays, take first element or aggregate
                    if (array.Count > 0)
                    {
                        current = array[0];
                    }
                    else
                    {
                        return null;
                    }
                }

                current = current[parts[i]];
            }

            // Convert to appropriate type
            if (current == null)
            {
                return null;
            }

            if (current is JValue jValue)
            {
                return jValue.Value;
            }

            // For complex objects, return string representation
            return current.ToString();
        }

        /// <summary>
        /// Simplifies path to use as dictionary key
        /// </summary>
        private string GetSimplifiedKey(string path)
        {
            var parts = path.Split('.');
            return parts.Length > 0 ? parts[parts.Length - 1] : path;
        }

        /// <summary>
        /// Groups records by identical quasi-identifier values
        /// </summary>
        private List<List<Dictionary<string, object>>> GroupByQuasiIdentifiers(List<Dictionary<string, object>> records)
        {
            // Group records by quasi-identifier signature
            var groups = new Dictionary<string, List<Dictionary<string, object>>>();

            foreach (var record in records)
            {
                var signature = CreateSignature(record);

                if (!groups.ContainsKey(signature))
                {
                    groups[signature] = new List<Dictionary<string, object>>();
                }

                groups[signature].Add(record);
            }

            // Convert to list of equivalence classes
            return groups.Values.ToList();
        }

        /// <summary>
        /// Creates a string signature from quasi-identifier values for grouping
        /// </summary>
        private string CreateSignature(Dictionary<string, object> quasiIdentifiers)
        {
            // Sort keys for consistent signatures
            var sortedKeys = quasiIdentifiers.Keys.OrderBy(k => k).ToList();
            var parts = sortedKeys.Select(key => $"{key}:{quasiIdentifiers[key]}");
            return string.Join("|", parts);
        }

        /// <summary>
        /// Builds equivalence classes from pre-extracted quasi-identifiers
        /// </summary>
        /// <param name="quasiIdentifierList">List of quasi-identifier dictionaries</param>
        /// <returns>Equivalence classes grouped by identical values</returns>
        public List<List<Dictionary<string, object>>> BuildFromQuasiIdentifiers(
            List<Dictionary<string, object>> quasiIdentifierList)
        {
            if (quasiIdentifierList == null || !quasiIdentifierList.Any())
            {
                return new List<List<Dictionary<string, object>>>();
            }

            return GroupByQuasiIdentifiers(quasiIdentifierList);
        }
    }
}
