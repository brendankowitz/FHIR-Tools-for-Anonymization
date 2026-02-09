using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.ElementModel;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Models
{
    public class ProcessResult
    {
        private readonly List<ProcessRecord> _processRecords = new List<ProcessRecord>();
        private readonly Dictionary<string, object> _privacyMetrics = new Dictionary<string, object>();
        private readonly Dictionary<string, List<ITypedElement>> _processRecordMap = new Dictionary<string, List<ITypedElement>>();
        
        public IReadOnlyCollection<ProcessRecord> ProcessRecords => _processRecords;
        public IReadOnlyDictionary<string, object> PrivacyMetrics => _privacyMetrics;

        /// <summary>
        /// Indicates if the resource satisfies k-anonymity
        /// </summary>
        public bool IsKAnonymized { get; private set; } = false;

        /// <summary>
        /// Indicates if the resource has been processed with differential privacy
        /// </summary>
        public bool IsDifferentiallyPrivate { get; private set; } = false;

        /// <summary>
        /// Indicates if any redact operation was performed
        /// </summary>
        public bool IsRedacted => _processRecords.Any(r => r.AnonymizationOperation == "redact");

        /// <summary>
        /// Indicates if any abstract operation was performed
        /// </summary>
        public bool IsAbstracted => _processRecords.Any(r => r.AnonymizationOperation == "abstract");

        /// <summary>
        /// Indicates if any cryptoHash operation was performed
        /// </summary>
        public bool IsCryptoHashed => _processRecords.Any(r => r.AnonymizationOperation == "cryptoHash");

        /// <summary>
        /// Indicates if any encrypt operation was performed
        /// </summary>
        public bool IsEncrypted => _processRecords.Any(r => r.AnonymizationOperation == "encrypt");

        /// <summary>
        /// Indicates if any perturb operation was performed
        /// </summary>
        public bool IsPerturbed => _processRecords.Any(r => r.AnonymizationOperation == "perturb");

        /// <summary>
        /// Indicates if any substitute operation was performed
        /// </summary>
        public bool IsSubstituted => _processRecords.Any(r => r.AnonymizationOperation == "substitute");

        /// <summary>
        /// Indicates if any generalize operation was performed
        /// </summary>
        public bool IsGeneralized => _processRecords.Any(r => r.AnonymizationOperation == "generalize");

        public void AddProcessRecord(string anonymizationOperation, ElementNode node)
        {
            _processRecords.Add(new ProcessRecord(anonymizationOperation, node));
            
            // Maintain backward compatibility with old map structure
            if (!_processRecordMap.ContainsKey(anonymizationOperation))
            {
                _processRecordMap[anonymizationOperation] = new List<ITypedElement>();
            }
            _processRecordMap[anonymizationOperation].Add(node);
        }

        public void AddPrivacyMetric(string key, object value)
        {
            _privacyMetrics[key] = value;
        }

        public void SetKAnonymized(bool value)
        {
            IsKAnonymized = value;
        }

        public void SetDifferentiallyPrivate(bool value)
        {
            IsDifferentiallyPrivate = value;
        }

        /// <summary>
        /// Merge another ProcessResult into this one
        /// </summary>
        public void Update(ProcessResult other)
        {
            if (other == null)
            {
                return;
            }

            foreach (var record in other._processRecords)
            {
                AddProcessRecord(record.AnonymizationOperation, record.Node);
            }

            foreach (var metric in other._privacyMetrics)
            {
                _privacyMetrics[metric.Key] = metric.Value;
            }

            if (other.IsKAnonymized)
            {
                IsKAnonymized = true;
            }

            if (other.IsDifferentiallyPrivate)
            {
                IsDifferentiallyPrivate = true;
            }
        }

        /// <summary>
        /// Get process records grouped by operation (for backward compatibility)
        /// </summary>
        /// <returns>Dictionary mapping operation names to lists of nodes</returns>
        public IReadOnlyDictionary<string, List<ITypedElement>> GetProcessRecordMap()
        {
            return _processRecordMap;
        }

        public override string ToString()
        {
            var operations = string.Join(", ", _processRecords.Select(r => r.AnonymizationOperation));
            var privacy = new List<string>();
            
            if (IsKAnonymized)
            {
                privacy.Add("K-Anonymized");
            }
            if (IsDifferentiallyPrivate)
            {
                privacy.Add("Differentially Private");
            }

            var privacyStr = privacy.Any() ? $" [{string.Join(", ", privacy)}]" : "";
            return $"ProcessResult: {_processRecords.Count} operations ({operations}){privacyStr}";
        }
    }

    public class ProcessRecord
    {
        public string AnonymizationOperation { get; }
        public ElementNode Node { get; }

        public ProcessRecord(string anonymizationOperation, ElementNode node)
        {
            AnonymizationOperation = anonymizationOperation;
            Node = node;
        }
    }
}
