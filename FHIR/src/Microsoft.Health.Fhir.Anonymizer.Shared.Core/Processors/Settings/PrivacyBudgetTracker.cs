using System;
using System.Collections.Concurrent;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings
{
    /// <summary>
    /// Tracks privacy budget (epsilon) consumption for differential privacy operations
    /// Implements basic sequential composition tracking
    /// Thread-safe for concurrent operations
    /// </summary>
    public class PrivacyBudgetTracker
    {
        private static readonly Lazy<PrivacyBudgetTracker> _instance = new Lazy<PrivacyBudgetTracker>(() => new PrivacyBudgetTracker());
        private readonly ConcurrentDictionary<string, double> _budgetsByContext = new ConcurrentDictionary<string, double>();
        private readonly ConcurrentDictionary<string, double> _totalBudgetsByContext = new ConcurrentDictionary<string, double>();
        private readonly object _lockObject = new object();

        /// <summary>
        /// Warning threshold as percentage of total budget (default: 80%)
        /// </summary>
        public double WarningThreshold { get; set; } = 0.8;

        private PrivacyBudgetTracker()
        {
        }

        public static PrivacyBudgetTracker Instance => _instance.Value;

        /// <summary>
        /// Initialize or reset budget for a specific context (e.g., file, session)
        /// </summary>
        /// <param name="context">Context identifier (e.g., filename, session ID)</param>
        /// <param name="totalBudget">Total epsilon budget available</param>
        public void InitializeBudget(string context, double totalBudget)
        {
            if (string.IsNullOrEmpty(context))
            {
                throw new ArgumentException("Budget context cannot be null or empty. Each dataset must have a unique context identifier.");
            }

            if (totalBudget <= 0)
            {
                throw new ArgumentException("Total budget must be greater than 0.");
            }

            _budgetsByContext[context] = 0.0;
            _totalBudgetsByContext[context] = totalBudget;
        }

        /// <summary>
        /// Consume epsilon budget for an operation (thread-safe)
        /// SECURITY FIX: No longer auto-initializes budget - must be explicitly set via InitializeBudget()
        /// </summary>
        /// <param name="context">Context identifier</param>
        /// <param name="epsilon">Epsilon value to consume</param>
        /// <returns>True if budget is available, false if budget would be exceeded</returns>
        public bool ConsumeBudget(string context, double epsilon)
        {
            if (string.IsNullOrEmpty(context))
            {
                throw new ArgumentException("Budget context cannot be null or empty.");
            }

            // SECURITY: Require explicit initialization - do NOT auto-initialize with default budget
            if (!_budgetsByContext.ContainsKey(context))
            {
                throw new InvalidOperationException($"Privacy budget not initialized for context '{context}'. Call InitializeBudget() before consuming budget to prevent unintended operations.");
            }

            // Use lock to ensure atomic check-and-update
            lock (_lockObject)
            {
                var currentBudget = _budgetsByContext[context];
                var totalBudget = _totalBudgetsByContext[context];
                var newBudget = currentBudget + epsilon;

                if (newBudget > totalBudget)
                {
                    return false;
                }

                _budgetsByContext[context] = newBudget;
                return true;
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
        /// </summary>
        public void ResetBudget(string context)
        {
            if (string.IsNullOrEmpty(context))
            {
                throw new ArgumentException("Budget context cannot be null or empty.");
            }

            if (_budgetsByContext.ContainsKey(context))
            {
                _budgetsByContext[context] = 0.0;
            }
        }

        /// <summary>
        /// Reset all budgets
        /// </summary>
        public void ResetAll()
        {
            _budgetsByContext.Clear();
            _totalBudgetsByContext.Clear();
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
    }
}
