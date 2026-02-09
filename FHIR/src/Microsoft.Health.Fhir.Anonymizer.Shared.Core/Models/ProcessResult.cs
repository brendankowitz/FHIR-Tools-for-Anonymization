using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.ElementModel;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Models
{
    public class ProcessResult
    {
        private readonly List<ProcessRecord> _processRecords = new List<ProcessRecord>();
        private readonly Dictionary<string, object> _privacyMetrics = new Dictionary<string, object>();
        
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

        public void AddProcessRecord(string anonymizationOperation, ElementNode node)
        {
            _processRecords.Add(new ProcessRecord(anonymizationOperation, node));
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
