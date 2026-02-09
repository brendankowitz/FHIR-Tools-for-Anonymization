using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings
{
    /// <summary>
    /// Tracks privacy budget (epsilon) consumption for differential privacy operations
    /// Implements basic sequential composition tracking
    /// Thread-safe for concurrent operations with comprehensive audit logging
    /// </summary>
    public class PrivacyBudgetTracker
    {
        private static readonly Lazy<PrivacyBudgetTracker> _instance = new Lazy<PrivacyBudgetTracker>(() => new PrivacyBudgetTracker());
        private readonly ConcurrentDictionary<string, double> _budgetsByContext = new ConcurrentDictionary<string, double>();
        private readonly ConcurrentDictionary<string, double> _totalBudgetsByContext = new ConcurrentDictionary<string, double>();
        private readonly ConcurrentDictionary<string, DateTime> _initializationTimestamps = new ConcurrentDictionary<string, DateTime>();
        private readonly ConcurrentDictionary<string, ConcurrentBag<BudgetAuditEntry>> _auditLogs = new ConcurrentDictionary<string, ConcurrentBag<BudgetAuditEntry>>();
        private readonly object _lockObject = new object();
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<PrivacyBudgetTracker>();

        /// <summary>
        /// Regex pattern for recommended budget context naming convention.
        /// Format: dataset-id:operation-id:timestamp or any alphanumeric with hyphens/underscores/colons
        /// Examples: "patient-cohort-2024:aggregation-001:20240115T103045Z", "file-12345:anonymize:20240115"
        /// </summary>
        private static readonly Regex ContextNamingPattern = new Regex(
            @"^[a-zA-Z0-9][a-zA-Z0-9_:\-]*[a-zA-Z0-9]$",
            RegexOptions.Compiled);

        /// <summary>
        /// Warning threshold as percentage of total budget (default: 80%)
        /// </summary>
        public double WarningThreshold { get; set; } = 0.8;

        /// <summary>
        /// When true, enforces strict context naming convention (default: false for backward compatibility)
        /// Recommended to enable in production for audit clarity
        /// </summary>
        public bool EnforceContextNamingConvention { get; set; } = false;

        private PrivacyBudgetTracker()
        {
        }

        public static PrivacyBudgetTracker Instance => _instance.Value;

        /// <summary>
        /// Validates budget context naming follows security best practices
        /// SECURITY: Well-structured context names improve audit trails and prevent confusion
        /// </summary>
        /// <param name="context">Context identifier to validate</param>
        /// <param name="throwOnInvalid">If true, throws exception on invalid names</param>
        /// <returns>True if context name is valid, false otherwise</returns>
        private bool ValidateContextNaming(string context, bool throwOnInvalid)
        {
            if (string.IsNullOrWhiteSpace(context))
            {
                if (throwOnInvalid)
                {
                    throw new ArgumentException("Budget context cannot be null or empty.");
                }
                return false;
            }

            // Check for dangerous patterns
            if (context.Contains("../") || context.Contains("..\\"))
            {
                var message = $"Budget context '{context}' contains path traversal patterns which are not allowed.";
                _logger.LogError($"[PRIVACY AUDIT SECURITY] {message}");
                if (throwOnInvalid)
                {
                    throw new System.Security.SecurityException(message);
                }
                return false;
            }

            // Check naming convention
            if (!ContextNamingPattern.IsMatch(context))
            {
                var message = $"Budget context '{context}' does not follow recommended naming convention. " +
                             "Use format: 'dataset-id:operation-id:timestamp' with alphanumeric characters, hyphens, underscores, and colons.";
                
                if (EnforceContextNamingConvention)
                {
                    _logger.LogError($"[PRIVACY AUDIT SECURITY] {message}");
                    if (throwOnInvalid)
                    {
                        throw new ArgumentException(message);
                    }
                    return false;
                }
                else
                {
                    _logger.LogWarning($"[PRIVACY AUDIT WARNING] {message}");
                }
            }

            return true;
        }

        /// <summary>
        /// Initialize or reset budget for a specific context (e.g., file, session)
        /// SECURITY: Logs initialization with timestamp for audit trail
        /// SECURITY: Validates context naming to prevent confusion and improve audit clarity
        /// </summary>
        /// <param name="context">Context identifier (recommended format: 'dataset-id:operation-id:timestamp')</param>
        /// <param name="totalBudget">Total epsilon budget available</param>
        public void InitializeBudget(string context, double totalBudget)
        {
            // SECURITY: Validate context naming
            ValidateContextNaming(context, throwOnInvalid: true);

            if (totalBudget <= 0)
            {
                throw new ArgumentException("Total budget must be greater than 0.");
            }

            var timestamp = DateTime.UtcNow;
            _budgetsByContext[context] = 0.0;
            _totalBudgetsByContext[context] = totalBudget;
            _initializationTimestamps[context] = timestamp;
            _auditLogs[context] = new ConcurrentBag<BudgetAuditEntry>();

            // SECURITY AUDIT: Log budget initialization
            var auditEntry = new BudgetAuditEntry
            {
                Timestamp = timestamp,
                Operation = "Initialize",
                Context = context,
                EpsilonConsumed = 0.0,
                TotalConsumed = 0.0,
                RemainingBudget = totalBudget,
                TotalBudget = totalBudget,
                Success = true
            };
            _auditLogs[context].Add(auditEntry);

            _logger.LogInformation($"[PRIVACY AUDIT] Budget initialized for context '{context}': Total={totalBudget}, Timestamp={timestamp:O}");
        }

        /// <summary>
        /// Consume epsilon budget for an operation (thread-safe with audit logging)
        /// SECURITY FIX: No longer auto-initializes budget - must be explicitly set via InitializeBudget()
        /// SECURITY AUDIT: All consumption attempts are logged with timestamps and outcomes
        /// THREAD-SAFETY: Uses lock for atomic check-and-update operations
        /// </summary>
        /// <param name="context">Context identifier</param>
        /// <param name="epsilon">Epsilon value to consume</param>
        /// <returns>True if budget is available, false if budget would be exceeded</returns>
        public bool ConsumeBudget(string context, double epsilon)
        {
            // SECURITY: Validate context naming
            ValidateContextNaming(context, throwOnInvalid: true);

            // SECURITY: Require explicit initialization - do NOT auto-initialize with default budget
            if (!_budgetsByContext.ContainsKey(context))
            {
                _logger.LogError($"[PRIVACY AUDIT] Attempted to consume budget for uninitialized context '{context}'");
                throw new InvalidOperationException($"Privacy budget not initialized for context '{context}'. Call InitializeBudget() before consuming budget to prevent unintended operations.");
            }

            var timestamp = DateTime.UtcNow;
            bool success;
            double remainingBefore, remainingAfter;

            // THREAD-SAFETY: Use lock to ensure atomic check-and-update
            lock (_lockObject)
            {
                var currentBudget = _budgetsByContext[context];
                var totalBudget = _totalBudgetsByContext[context];
                var newBudget = currentBudget + epsilon;

                remainingBefore = Math.Max(0, totalBudget - currentBudget);

                if (newBudget > totalBudget)
                {
                    success = false;
                    remainingAfter = remainingBefore;
                }
                else
                {
                    _budgetsByContext[context] = newBudget;
                    success = true;
                    remainingAfter = Math.Max(0, totalBudget - newBudget);
                }

                // SECURITY AUDIT: Log every consumption attempt
                var auditEntry = new BudgetAuditEntry
                {
                    Timestamp = timestamp,
                    Operation = "Consume",
                    Context = context,
                    EpsilonConsumed = epsilon,
                    TotalConsumed = success ? newBudget : currentBudget,
                    RemainingBudget = remainingAfter,
                    TotalBudget = totalBudget,
                    Success = success
                };

                if (_auditLogs.TryGetValue(context, out var auditLog))
                {
                    auditLog.Add(auditEntry);
                }

                // Log to standard logger
                if (success)
                {
                    _logger.LogInformation(
                        $"[PRIVACY AUDIT] Budget consumed for context '{context}': " +
                        $"Epsilon={epsilon:F6}, Total={newBudget:F6}/{totalBudget:F6}, " +
                        $"Remaining={remainingAfter:F6}, Timestamp={timestamp:O}");

                    // Warn if approaching limit
                    if (newBudget >= (totalBudget * WarningThreshold))
                    {
                        _logger.LogWarning(
                            $"[PRIVACY AUDIT WARNING] Budget approaching limit for context '{context}': " +
                            $"Consumed={newBudget:F6}/{totalBudget:F6} ({(newBudget / totalBudget * 100):F1}%), " +
                            $"Remaining={remainingAfter:F6}");
                    }
                }
                else
                {
                    _logger.LogError(
                        $"[PRIVACY AUDIT DENIED] Budget exceeded for context '{context}': " +
                        $"Attempted={epsilon:F6}, Would total={newBudget:F6}, " +
                        $"Limit={totalBudget:F6}, Remaining={remainingBefore:F6}, Timestamp={timestamp:O}");
                }

                return success;
            }
        }

        /// <summary>
        /// Get remaining budget for a context
        /// </summary>
        public double GetRemainingBudget(string context)
        {
            if (string.IsNullOrEmpty(context))
            {
                throw new ArgumentException("Budget context cannot be null or empty.");
            }

            if (!_budgetsByContext.ContainsKey(context))
            {
                throw new InvalidOperationException($"Privacy budget not initialized for context '{context}'.");
            }

            var consumed = _budgetsByContext[context];
            var total = _totalBudgetsByContext[context];
            return Math.Max(0, total - consumed);
        }

        /// <summary>
        /// Get consumed budget for a context
        /// </summary>
        public double GetConsumedBudget(string context)
        {
            if (string.IsNullOrEmpty(context))
            {
                throw new ArgumentException("Budget context cannot be null or empty.");
            }

            return _budgetsByContext.GetValueOrDefault(context, 0.0);
        }

        /// <summary>
        /// Check if budget consumption is approaching the warning threshold
        /// </summary>
        public bool IsApproachingLimit(string context)
        {
            if (string.IsNullOrEmpty(context))
            {
                return false;
            }

            if (!_budgetsByContext.ContainsKey(context))
            {
                return false;
            }

            var consumed = _budgetsByContext[context];
            var total = _totalBudgetsByContext[context];

            if (double.IsInfinity(total))
            {
                return false;
            }

            return consumed >= (total * WarningThreshold);
        }

        /// <summary>
        /// Reset budget for a specific context
        /// SECURITY AUDIT: Logs budget reset with timestamp
        /// </summary>
        public void ResetBudget(string context)
        {
            if (string.IsNullOrEmpty(context))
            {
                throw new ArgumentException("Budget context cannot be null or empty.");
            }

            if (_budgetsByContext.ContainsKey(context))
            {
                var timestamp = DateTime.UtcNow;
                var totalBudget = _totalBudgetsByContext[context];
                _budgetsByContext[context] = 0.0;

                // SECURITY AUDIT: Log budget reset
                var auditEntry = new BudgetAuditEntry
                {
                    Timestamp = timestamp,
                    Operation = "Reset",
                    Context = context,
                    EpsilonConsumed = 0.0,
                    TotalConsumed = 0.0,
                    RemainingBudget = totalBudget,
                    TotalBudget = totalBudget,
                    Success = true
                };

                if (_auditLogs.TryGetValue(context, out var auditLog))
                {
                    auditLog.Add(auditEntry);
                }

                _logger.LogInformation($"[PRIVACY AUDIT] Budget reset for context '{context}': Timestamp={timestamp:O}");
            }
        }

        /// <summary>
        /// Reset all budgets
        /// SECURITY AUDIT: Logs global reset
        /// </summary>
        public void ResetAll()
        {
            var timestamp = DateTime.UtcNow;
            _budgetsByContext.Clear();
            _totalBudgetsByContext.Clear();
            _initializationTimestamps.Clear();
            _auditLogs.Clear();

            _logger.LogWarning($"[PRIVACY AUDIT] All budgets reset globally: Timestamp={timestamp:O}");
        }

        /// <summary>
        /// Get budget utilization percentage (0-1)
        /// </summary>
        public double GetUtilization(string context)
        {
            if (string.IsNullOrEmpty(context))
            {
                return 0.0;
            }

            if (!_budgetsByContext.ContainsKey(context))
            {
                return 0.0;
            }

            var consumed = _budgetsByContext[context];
            var total = _totalBudgetsByContext[context];

            if (double.IsInfinity(total) || total == 0)
            {
                return 0.0;
            }

            return Math.Min(1.0, consumed / total);
        }

        /// <summary>
        /// Check if budget is initialized for a context
        /// </summary>
        public bool IsInitialized(string context)
        {
            if (string.IsNullOrEmpty(context))
            {
                return false;
            }

            return _budgetsByContext.ContainsKey(context);
        }

        /// <summary>
        /// Get audit log for a specific context
        /// SECURITY: Provides complete audit trail for compliance and forensics
        /// </summary>
        /// <param name="context">Context identifier</param>
        /// <returns>Read-only collection of audit entries</returns>
        public System.Collections.Generic.IReadOnlyCollection<BudgetAuditEntry> GetAuditLog(string context)
        {
            if (string.IsNullOrEmpty(context))
            {
                throw new ArgumentException("Budget context cannot be null or empty.");
            }

            if (_auditLogs.TryGetValue(context, out var auditLog))
            {
                return auditLog.ToArray();
            }

            return Array.Empty<BudgetAuditEntry>();
        }

        /// <summary>
        /// Get initialization timestamp for a context
        /// </summary>
        public DateTime? GetInitializationTimestamp(string context)
        {
            if (string.IsNullOrEmpty(context))
            {
                return null;
            }

            if (_initializationTimestamps.TryGetValue(context, out var timestamp))
            {
                return timestamp;
            }

            return null;
        }
    }

    /// <summary>
    /// Audit log entry for privacy budget operations
    /// Immutable record for compliance and forensic analysis
    /// </summary>
    public class BudgetAuditEntry
    {
        /// <summary>
        /// UTC timestamp of the operation
        /// </summary>
        public DateTime Timestamp { get; init; }

        /// <summary>
        /// Operation type: Initialize, Consume, Reset
        /// </summary>
        public string Operation { get; init; }

        /// <summary>
        /// Budget context identifier
        /// </summary>
        public string Context { get; init; }

        /// <summary>
        /// Epsilon value attempted to consume (for Consume operations)
        /// </summary>
        public double EpsilonConsumed { get; init; }

        /// <summary>
        /// Total epsilon consumed after this operation
        /// </summary>
        public double TotalConsumed { get; init; }

        /// <summary>
        /// Remaining budget after this operation
        /// </summary>
        public double RemainingBudget { get; init; }

        /// <summary>
        /// Total budget for the context
        /// </summary>
        public double TotalBudget { get; init; }

        /// <summary>
        /// Whether the operation succeeded (false for denied consumption attempts)
        /// </summary>
        public bool Success { get; init; }
    }
}
