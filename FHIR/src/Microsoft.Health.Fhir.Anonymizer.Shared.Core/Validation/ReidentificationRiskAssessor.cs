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
    /// Assesses reidentification risk for anonymized datasets.
    /// Implements standard risk metrics from privacy literature:
    /// - Prosecutor risk (maximum individual risk)
    /// - Journalist risk (average individual risk)
    /// - Marketer risk (proportion of population at risk)
    /// </summary>
    public class ReidentificationRiskAssessor
    {
        private const double HighRiskThreshold = 0.20;  // 20% or higher
        private const double MediumRiskThreshold = 0.10; // 10-20%
        // Below 10% is considered low risk

        /// <summary>
        /// Assesses reidentification risk for a collection of process results.
        /// </summary>
        /// <param name="results">Collection of process results containing privacy metrics</param>
        /// <returns>Risk assessment report with detailed metrics</returns>
        public ReidentificationRiskReport AssessRisk(IEnumerable<ProcessResult> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            var report = new ReidentificationRiskReport
            {
                TotalRecords = results.Count()
            };

            if (report.TotalRecords == 0)
            {
                report.RiskLevel = RiskLevel.Low;
                report.Summary = "No records to assess.";
                return report;
            }

            // Group results by equivalence class
            var equivalenceClasses = new Dictionary<string, int>();

            foreach (var result in results)
            {
                if (result?.PrivacyMetrics == null)
                {
                    continue;
                }

                if (result.PrivacyMetrics.TryGetValue("EquivalenceClassId", out var classIdObj) && 
                    classIdObj is string classId)
                {
                    if (!equivalenceClasses.ContainsKey(classId))
                    {
                        equivalenceClasses[classId] = 0;
                    }
                    equivalenceClasses[classId]++;
                }
            }

            if (equivalenceClasses.Count == 0)
            {
                // No equivalence classes - assume each record is unique (highest risk)
                report.ProsecutorRisk = 1.0;
                report.JournalistRisk = 1.0;
                report.MarketerRisk = 1.0;
                report.UniquenessRatio = 1.0;
                report.RiskLevel = RiskLevel.High;
                report.Summary = "No equivalence classes found. Each record may be uniquely identifiable.";
                return report;
            }

            return AssessRiskFromGroups(equivalenceClasses);
        }

        /// <summary>
        /// Assesses reidentification risk from grouped quasi-identifier data.
        /// </summary>
        /// <param name="quasiIdentifierGroups">Dictionary mapping quasi-identifier combinations to record counts</param>
        /// <returns>Risk assessment report</returns>
        public ReidentificationRiskReport AssessRiskFromGroups(Dictionary<string, int> quasiIdentifierGroups)
        {
            if (quasiIdentifierGroups == null)
            {
                throw new ArgumentNullException(nameof(quasiIdentifierGroups));
            }

            var report = new ReidentificationRiskReport
            {
                TotalRecords = quasiIdentifierGroups.Values.Sum(),
                EquivalenceClassCount = quasiIdentifierGroups.Count
            };

            if (report.TotalRecords == 0)
            {
                report.RiskLevel = RiskLevel.Low;
                report.Summary = "No records to assess.";
                return report;
            }

            // Calculate individual risks for each equivalence class
            var individualRisks = quasiIdentifierGroups.Values
                .Select(count => 1.0 / count)
                .ToList();

            // Prosecutor Risk: Maximum individual risk
            // Represents the risk if an attacker knows the victim is in the dataset
            report.ProsecutorRisk = individualRisks.Max();

            // Journalist Risk: Average individual risk
            // Represents the average risk across all records
            report.JournalistRisk = individualRisks.Average();

            // Marketer Risk: Proportion of records with high individual risk
            // Represents the proportion of population at high risk (typically unique records)
            var highRiskRecords = quasiIdentifierGroups
                .Where(kvp => 1.0 / kvp.Value >= HighRiskThreshold)
                .Sum(kvp => kvp.Value);
            report.MarketerRisk = (double)highRiskRecords / report.TotalRecords;

            // Uniqueness Ratio: Proportion of equivalence classes with minimum size
            var minClassSize = quasiIdentifierGroups.Values.Min();
            var minSizeClasses = quasiIdentifierGroups.Count(kvp => kvp.Value == minClassSize);
            report.UniquenessRatio = (double)minSizeClasses / report.EquivalenceClassCount;

            // Count unique records (equivalence classes of size 1)
            report.UniqueRecordCount = quasiIdentifierGroups.Count(kvp => kvp.Value == 1);
            report.UniqueRecordPercentage = (double)report.UniqueRecordCount / report.EquivalenceClassCount * 100;

            // Overall Risk Level Assessment
            report.RiskLevel = DetermineOverallRiskLevel(
                report.ProsecutorRisk, 
                report.JournalistRisk, 
                report.MarketerRisk);

            // Risk classification for each metric
            report.ProsecutorRiskLevel = ClassifyRisk(report.ProsecutorRisk);
            report.JournalistRiskLevel = ClassifyRisk(report.JournalistRisk);
            report.MarketerRiskLevel = ClassifyRisk(report.MarketerRisk);

            // Generate summary
            report.Summary = GenerateSummary(report);

            // Generate recommendations
            report.Recommendations = GenerateRecommendations(report, quasiIdentifierGroups);

            return report;
        }

        private RiskLevel DetermineOverallRiskLevel(double prosecutorRisk, double journalistRisk, double marketerRisk)
        {
            // Use the highest risk level among all metrics
            var risks = new[] { prosecutorRisk, journalistRisk, marketerRisk };
            var maxRisk = risks.Max();

            if (maxRisk >= HighRiskThreshold)
            {
                return RiskLevel.High;
            }
            else if (maxRisk >= MediumRiskThreshold)
            {
                return RiskLevel.Medium;
            }
            else
            {
                return RiskLevel.Low;
            }
        }

        private RiskLevel ClassifyRisk(double riskValue)
        {
            if (riskValue >= HighRiskThreshold)
            {
                return RiskLevel.High;
            }
            else if (riskValue >= MediumRiskThreshold)
            {
                return RiskLevel.Medium;
            }
            else
            {
                return RiskLevel.Low;
            }
        }

        private string GenerateSummary(ReidentificationRiskReport report)
        {
            var summary = $"Overall reidentification risk: {report.RiskLevel}. ";

            if (report.RiskLevel == RiskLevel.High)
            {
                summary += $"The dataset has significant reidentification risk. ";
                
                if (report.UniqueRecordCount > 0)
                {
                    summary += $"Found {report.UniqueRecordCount} unique records ({report.UniqueRecordPercentage:F1}% of equivalence classes). ";
                }

                summary += $"Prosecutor risk: {report.ProsecutorRisk:P1}, Journalist risk: {report.JournalistRisk:P1}.";
            }
            else if (report.RiskLevel == RiskLevel.Medium)
            {
                summary += $"The dataset has moderate reidentification risk. ";
                summary += $"Prosecutor risk: {report.ProsecutorRisk:P1}, Journalist risk: {report.JournalistRisk:P1}.";
            }
            else
            {
                summary += $"The dataset has low reidentification risk. ";
                summary += $"All metrics are below {MediumRiskThreshold:P0} threshold.";
            }

            return summary;
        }

        private List<string> GenerateRecommendations(ReidentificationRiskReport report, Dictionary<string, int> groups)
        {
            var recommendations = new List<string>();

            if (report.RiskLevel == RiskLevel.High)
            {
                if (report.UniqueRecordCount > 0)
                {
                    recommendations.Add($"Suppress or further generalize {report.UniqueRecordCount} unique equivalence classes.");
                }

                if (report.ProsecutorRisk >= HighRiskThreshold)
                {
                    var minClassSize = groups.Values.Min();
                    recommendations.Add($"Increase k-value from {minClassSize} to at least {(int)(1.0 / MediumRiskThreshold)} to reduce prosecutor risk.");
                }

                recommendations.Add("Consider additional generalization of quasi-identifiers to increase equivalence class sizes.");
                recommendations.Add("Review data utility requirements - higher privacy may require accepting some data quality loss.");
            }
            else if (report.RiskLevel == RiskLevel.Medium)
            {
                recommendations.Add("Consider increasing k-value or applying additional generalization for higher privacy.");
                
                if (report.UniqueRecordCount > 0)
                {
                    recommendations.Add($"Review {report.UniqueRecordCount} unique equivalence classes for potential suppression.");
                }

                recommendations.Add("Current risk level may be acceptable for internal use, but review requirements for external data sharing.");
            }
            else
            {
                recommendations.Add("Current anonymization provides good privacy protection.");
                recommendations.Add("Monitor risk metrics if dataset is updated or combined with other data sources.");
            }

            return recommendations;
        }
    }

    /// <summary>
    /// Report containing reidentification risk assessment results.
    /// </summary>
    public class ReidentificationRiskReport
    {
        /// <summary>
        /// Overall risk level classification.
        /// </summary>
        public RiskLevel RiskLevel { get; set; }

        /// <summary>
        /// Total number of records assessed.
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// Number of equivalence classes.
        /// </summary>
        public int EquivalenceClassCount { get; set; }

        /// <summary>
        /// Prosecutor risk: Maximum individual reidentification risk.
        /// Value between 0 and 1, where 1 means certain reidentification.
        /// </summary>
        public double ProsecutorRisk { get; set; }

        /// <summary>
        /// Risk level classification for prosecutor risk.
        /// </summary>
        public RiskLevel ProsecutorRiskLevel { get; set; }

        /// <summary>
        /// Journalist risk: Average individual reidentification risk.
        /// Value between 0 and 1.
        /// </summary>
        public double JournalistRisk { get; set; }

        /// <summary>
        /// Risk level classification for journalist risk.
        /// </summary>
        public RiskLevel JournalistRiskLevel { get; set; }

        /// <summary>
        /// Marketer risk: Proportion of records with high individual risk.
        /// Value between 0 and 1.
        /// </summary>
        public double MarketerRisk { get; set; }

        /// <summary>
        /// Risk level classification for marketer risk.
        /// </summary>
        public RiskLevel MarketerRiskLevel { get; set; }

        /// <summary>
        /// Ratio of equivalence classes with minimum size to total classes.
        /// Higher values indicate more uniform distribution (lower risk).
        /// </summary>
        public double UniquenessRatio { get; set; }

        /// <summary>
        /// Number of unique records (equivalence classes of size 1).
        /// </summary>
        public int UniqueRecordCount { get; set; }

        /// <summary>
        /// Percentage of equivalence classes that are unique.
        /// </summary>
        public double UniqueRecordPercentage { get; set; }

        /// <summary>
        /// Human-readable summary of the risk assessment.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// List of recommendations to reduce reidentification risk.
        /// </summary>
        public List<string> Recommendations { get; set; } = new List<string>();
    }

    /// <summary>
    /// Classification of reidentification risk levels.
    /// </summary>
    public enum RiskLevel
    {
        /// <summary>
        /// Low risk: Risk metrics below 10%.
        /// </summary>
        Low,

        /// <summary>
        /// Medium risk: Risk metrics between 10% and 20%.
        /// </summary>
        Medium,

        /// <summary>
        /// High risk: Risk metrics 20% or higher.
        /// </summary>
        High
    }
}
