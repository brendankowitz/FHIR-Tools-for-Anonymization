// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Validation
{
    /// <summary>
    /// Validates that a dataset satisfies k-anonymity property.
    /// K-anonymity requires that each combination of quasi-identifiers appears
    /// in at least k records, preventing individual identification.
    /// </summary>
    public class KAnonymityValidator
    {
        /// <summary>
        /// Validates k-anonymity property for a collection of process results.
        /// </summary>
        /// <param name="results">Collection of process results containing privacy metrics</param>
        /// <param name="requiredK">Required k-value for validation</param>
        /// <returns>Validation report with statistics and violations</returns>
        public KAnonymityValidationReport Validate(IEnumerable<ProcessResult> results, int requiredK)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            if (requiredK < 2)
            {
                throw new ArgumentException("K-value must be at least 2", nameof(requiredK));
            }

            var report = new KAnonymityValidationReport
            {
                RequiredK = requiredK,
                TotalRecords = results.Count()
            };

            if (report.TotalRecords == 0)
            {
                report.IsValid = true;
                return report;
            }

            // Group results by equivalence class identifier
            var equivalenceClasses = new Dictionary<string, List<ProcessResult>>();

            foreach (var result in results)
            {
                if (result?.PrivacyMetrics == null)
                {
                    continue;
                }

                // Get equivalence class ID from privacy metrics
                if (result.PrivacyMetrics.TryGetValue("EquivalenceClassId", out var classIdObj) && 
                    classIdObj is string classId)
                {
                    if (!equivalenceClasses.ContainsKey(classId))
                    {
                        equivalenceClasses[classId] = new List<ProcessResult>();
                    }
                    equivalenceClasses[classId].Add(result);
                }
            }

            report.EquivalenceClassCount = equivalenceClasses.Count;

            if (report.EquivalenceClassCount == 0)
            {
                // No equivalence classes found - data may not be k-anonymized
                report.IsValid = false;
                report.ValidationMessage = "No equivalence classes found in process results. Ensure data has been processed with k-anonymity.";
                return report;
            }

            // Analyze each equivalence class
            var classSizes = new List<int>();
            var violations = new List<KAnonymityViolation>();

            foreach (var kvp in equivalenceClasses)
            {
                var classSize = kvp.Value.Count;
                classSizes.Add(classSize);

                if (classSize < requiredK)
                {
                    violations.Add(new KAnonymityViolation
                    {
                        EquivalenceClassId = kvp.Key,
                        ActualSize = classSize,
                        RequiredSize = requiredK,
                        ShortfallCount = requiredK - classSize
                    });
                }
            }

            report.Violations = violations;
            report.ViolationCount = violations.Count;
            report.IsValid = violations.Count == 0;

            // Calculate statistics
            if (classSizes.Any())
            {
                report.MinimumClassSize = classSizes.Min();
                report.MaximumClassSize = classSizes.Max();
                report.AverageClassSize = classSizes.Average();
                report.MedianClassSize = CalculateMedian(classSizes);

                // Calculate distribution
                report.ClassSizeDistribution = classSizes
                    .GroupBy(size => size)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Calculate percentage of records in valid equivalence classes
                var recordsInValidClasses = equivalenceClasses
                    .Where(kvp => kvp.Value.Count >= requiredK)
                    .Sum(kvp => kvp.Value.Count);
                report.RecordsInValidClassesPercentage = 
                    (double)recordsInValidClasses / report.TotalRecords * 100;
            }

            // Generate validation message
            if (report.IsValid)
            {
                report.ValidationMessage = $"Dataset satisfies {requiredK}-anonymity. " +
                    $"All {report.EquivalenceClassCount} equivalence classes have at least {requiredK} records.";
            }
            else
            {
                report.ValidationMessage = $"Dataset does NOT satisfy {requiredK}-anonymity. " +
                    $"Found {report.ViolationCount} equivalence classes with fewer than {requiredK} records. " +
                    $"Only {report.RecordsInValidClassesPercentage:F1}% of records are in valid equivalence classes.";
            }

            return report;
        }

        /// <summary>
        /// Validates k-anonymity for grouped quasi-identifier data.
        /// </summary>
        /// <param name="quasiIdentifierGroups">Dictionary mapping quasi-identifier combinations to record counts</param>
        /// <param name="requiredK">Required k-value</param>
        /// <returns>Validation report</returns>
        public KAnonymityValidationReport ValidateFromGroups(
            Dictionary<string, int> quasiIdentifierGroups, 
            int requiredK)
        {
            if (quasiIdentifierGroups == null)
            {
                throw new ArgumentNullException(nameof(quasiIdentifierGroups));
            }

            if (requiredK < 2)
            {
                throw new ArgumentException("K-value must be at least 2", nameof(requiredK));
            }

            var report = new KAnonymityValidationReport
            {
                RequiredK = requiredK,
                EquivalenceClassCount = quasiIdentifierGroups.Count,
                TotalRecords = quasiIdentifierGroups.Values.Sum()
            };

            if (report.TotalRecords == 0)
            {
                report.IsValid = true;
                return report;
            }

            var violations = new List<KAnonymityViolation>();
            var classSizes = quasiIdentifierGroups.Values.ToList();

            foreach (var kvp in quasiIdentifierGroups)
            {
                if (kvp.Value < requiredK)
                {
                    violations.Add(new KAnonymityViolation
                    {
                        EquivalenceClassId = kvp.Key,
                        ActualSize = kvp.Value,
                        RequiredSize = requiredK,
                        ShortfallCount = requiredK - kvp.Value
                    });
                }
            }

            report.Violations = violations;
            report.ViolationCount = violations.Count;
            report.IsValid = violations.Count == 0;

            // Calculate statistics
            report.MinimumClassSize = classSizes.Min();
            report.MaximumClassSize = classSizes.Max();
            report.AverageClassSize = classSizes.Average();
            report.MedianClassSize = CalculateMedian(classSizes);

            report.ClassSizeDistribution = classSizes
                .GroupBy(size => size)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());

            var recordsInValidClasses = quasiIdentifierGroups
                .Where(kvp => kvp.Value >= requiredK)
                .Sum(kvp => kvp.Value);
            report.RecordsInValidClassesPercentage = 
                (double)recordsInValidClasses / report.TotalRecords * 100;

            if (report.IsValid)
            {
                report.ValidationMessage = $"Dataset satisfies {requiredK}-anonymity.";
            }
            else
            {
                report.ValidationMessage = $"Dataset does NOT satisfy {requiredK}-anonymity. " +
                    $"Found {report.ViolationCount} violations.";
            }

            return report;
        }

        private double CalculateMedian(List<int> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            int count = sorted.Count;

            if (count % 2 == 0)
            {
                return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
            }
            else
            {
                return sorted[count / 2];
            }
        }
    }

    /// <summary>
    /// Report containing k-anonymity validation results and statistics.
    /// </summary>
    public class KAnonymityValidationReport
    {
        /// <summary>
        /// Whether the dataset satisfies k-anonymity.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Required k-value for validation.
        /// </summary>
        public int RequiredK { get; set; }

        /// <summary>
        /// Total number of records analyzed.
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// Number of equivalence classes found.
        /// </summary>
        public int EquivalenceClassCount { get; set; }

        /// <summary>
        /// Number of violations (equivalence classes smaller than k).
        /// </summary>
        public int ViolationCount { get; set; }

        /// <summary>
        /// List of k-anonymity violations.
        /// </summary>
        public List<KAnonymityViolation> Violations { get; set; } = new List<KAnonymityViolation>();

        /// <summary>
        /// Minimum equivalence class size.
        /// </summary>
        public int MinimumClassSize { get; set; }

        /// <summary>
        /// Maximum equivalence class size.
        /// </summary>
        public int MaximumClassSize { get; set; }

        /// <summary>
        /// Average equivalence class size.
        /// </summary>
        public double AverageClassSize { get; set; }

        /// <summary>
        /// Median equivalence class size.
        /// </summary>
        public double MedianClassSize { get; set; }

        /// <summary>
        /// Distribution of equivalence class sizes (size -> count).
        /// </summary>
        public Dictionary<int, int> ClassSizeDistribution { get; set; } = new Dictionary<int, int>();

        /// <summary>
        /// Percentage of records in valid equivalence classes (>= k).
        /// </summary>
        public double RecordsInValidClassesPercentage { get; set; }

        /// <summary>
        /// Human-readable validation message.
        /// </summary>
        public string ValidationMessage { get; set; }
    }

    /// <summary>
    /// Represents a k-anonymity violation.
    /// </summary>
    public class KAnonymityViolation
    {
        /// <summary>
        /// Identifier of the equivalence class that violates k-anonymity.
        /// </summary>
        public string EquivalenceClassId { get; set; }

        /// <summary>
        /// Actual size of the equivalence class.
        /// </summary>
        public int ActualSize { get; set; }

        /// <summary>
        /// Required minimum size (k-value).
        /// </summary>
        public int RequiredSize { get; set; }

        /// <summary>
        /// Number of additional records needed to satisfy k-anonymity.
        /// </summary>
        public int ShortfallCount { get; set; }
    }
}
