# Differential Privacy Configuration Guide

## Overview

Differential privacy is a rigorous mathematical framework that provides provable privacy guarantees by adding carefully calibrated noise to data. It protects individual privacy while enabling accurate aggregate analysis.

### Key Principle

**Definition**: A mechanism M provides ε-differential privacy if for any two datasets D1 and D2 that differ by one record, and for any possible output S:

```
Pr[M(D1) ∈ S] ≤ e^ε × Pr[M(D2) ∈ S]
```

This means an adversary cannot determine whether any specific individual's data was included in the dataset.

## Configuration Parameters

### Basic Configuration

```json
{
  "path": "Observation.valueQuantity.value",
  "method": "differentialPrivacy",
  "epsilon": 0.1,
  "delta": 0.00001,
  "mechanism": "laplace",
  "sensitivity": 1.0
}
```

### Parameters Explained

#### `epsilon` (ε) - Privacy Budget

**Definition**: Controls the privacy-utility tradeoff. Smaller ε = stronger privacy but more noise.

**Recommended Values**:
- **ε ≤ 0.1**: Strong privacy (significant noise)
- **ε = 0.5 - 1.0**: Moderate privacy (research standard)
- **ε = 1.0 - 5.0**: Weak privacy (better utility)
- **ε > 10**: Minimal privacy (approaching no privacy)

**Guidelines**:
- Total ε budget for entire dataset should be < 1.0 for strong privacy
- Each query/operation consumes part of the budget
- Once budget is exhausted, no more queries should be answered

#### `delta` (δ) - Failure Probability

**Definition**: Probability that privacy guarantee fails. Must be very small.

**Recommended Values**:
- δ < 1/n where n = dataset size
- Typical: δ = 10^-5 to 10^-7
- Never use δ > 0.01

**Purpose**: Allows using Gaussian mechanism which adds less noise than Laplace for same ε.

#### `mechanism` - Noise Distribution

**Laplace Mechanism**:
- Pure ε-differential privacy (δ = 0)
- Noise ~ Laplace(sensitivity/ε)
- Best for: Numerical data, count queries, sums
- Simpler guarantees, more widely understood

**Gaussian Mechanism**:
- (ε, δ)-differential privacy
- Noise ~ Normal(0, σ^2) where σ = sensitivity × √(2 ln(1.25/δ)) / ε
- Best for: Large datasets, when δ budget available
- Less noise than Laplace for same privacy level

**Exponential Mechanism** (future support):
- For categorical/non-numerical data
- Randomized response technique
- Best for: Rare categories, boolean data

#### `sensitivity` (Δf) - Global Sensitivity

**Definition**: Maximum amount that the query result can change by adding/removing one record.

**Examples**:
- Count queries: Sensitivity = 1 (one person can change count by ±1)
- Sum of ages (0-120): Sensitivity = 120
- Average of values [0, 100]: Sensitivity = 100/n (but often use worst-case)
- Median: High sensitivity (harder to protect)

**Guidelines**:
- Must be determined based on data bounds
- Underestimating sensitivity violates privacy guarantees
- Overestimating sensitivity adds unnecessary noise
- Use domain knowledge to set reasonable bounds

## Complete Example

### Protecting Observation Values

```json
{
  "fhirVersion": "R4",
  "processingErrors": "raise",
  "fhirPathRules": [
    {
      "path": "Observation.valueQuantity.value",
      "method": "differentialPrivacy",
      "epsilon": 0.1,
      "delta": 0.00001,
      "mechanism": "laplace",
      "sensitivity": 1.0,
      "description": "Lab test results with DP"
    },
    {
      "path": "Observation.component.valueQuantity.value",
      "method": "differentialPrivacy",
      "epsilon": 0.1,
      "delta": 0.00001,
      "mechanism": "laplace",
      "sensitivity": 1.0
    }
  ],
  "parameters": {
    "privacyBudget": {
      "totalEpsilon": 1.0,
      "warnThreshold": 0.8
    }
  }
}
```

## Privacy Budget Management

### Composition Theorem

When multiple differentially private operations are performed:

**Sequential Composition**:
- If M1 provides ε1-DP and M2 provides ε2-DP (on same data)
- Together they provide (ε1 + ε2)-DP
- **Budget adds up linearly**

**Parallel Composition**:
- If M1 operates on data subset A and M2 on disjoint subset B
- Together they provide max(ε1, ε2)-DP
- **Budget is the maximum, not sum**

### Budget Tracking

The `PrivacyBudgetTracker` manages epsilon consumption:

```csharp
// Initialize budget
PrivacyBudgetTracker.InitializeBudget(totalBudget: 1.0, warnThreshold: 0.8);

// Each operation consumes budget
tracker.ConsumeEpsilon(0.1); // Returns true if under budget

// Check remaining budget
var remaining = tracker.RemainingEpsilon; // 0.9
var consumed = tracker.ConsumedEpsilon;   // 0.1
var warnings = tracker.Warnings;          // Empty if < 80% used
```

### Budget Allocation Strategy

**Example**: Total budget = 1.0 for dataset export

1. **Identify operations**:
   - Anonymize patient ages: ε = 0.2
   - Anonymize lab values: ε = 0.3
   - Anonymize vital signs: ε = 0.3
   - Reserve for future queries: ε = 0.2

2. **Prioritize by sensitivity**:
   - Highest risk identifiers get most budget
   - Common queries get more budget than rare ones

3. **Monitor consumption**:
   - Set warning at 80% (ε = 0.8)
   - Stop all operations at 100% (ε = 1.0)

## Choosing Parameters

### Privacy Level Selection

| Use Case | Epsilon | Delta | Rationale |
|----------|---------|-------|--------|
| Public release (highly sensitive) | 0.1 - 0.5 | 10^-7 | Strong privacy, accept utility loss |
| Research dataset (moderate risk) | 0.5 - 2.0 | 10^-5 | Balance privacy and utility |
| Internal analytics (low risk) | 2.0 - 5.0 | 10^-4 | Prioritize utility |
| Development/testing | 5.0 - 10.0 | 10^-3 | Minimal privacy needed |

### Sensitivity Calculation

**Step 1**: Identify the query/function
- Example: Sum of glucose readings

**Step 2**: Determine data bounds
- Glucose range: 0 - 500 mg/dL (clinical maximum)

**Step 3**: Calculate worst-case change
- Adding/removing one record changes sum by at most 500
- **Sensitivity = 500**

**Step 4**: Adjust for data normalization
- If values normalized to [0, 1]: Sensitivity = 1.0
- If computing average instead of sum: Sensitivity = 500/n

### Mechanism Selection

**Use Laplace when**:
- δ budget not available
- Pure ε-DP required by policy
- Simpler guarantees preferred
- Small to medium datasets

**Use Gaussian when**:
- Large datasets (n > 10,000)
- δ budget available
- Need better utility with same ε
- Complex queries with composition

## Validation and Testing

### Noise Distribution Verification

```csharp
// Test noise properties
var processor = new DifferentialPrivacyProcessor(settings);
var noises = new List<double>();

for (int i = 0; i < 10000; i++)
{
    var noise = processor.GenerateNoise();
    noises.Add(noise);
}

// Verify mean ≈ 0
var mean = noises.Average(); // Should be ~0

// Verify scale matches expected
var expectedScale = sensitivity / epsilon;
var actualScale = /* compute from noises */;
```

### Privacy Guarantee Testing

**Important**: Cannot directly test that privacy is preserved (would require knowing true data).

**What to test**:
- Noise is added (output ≠ input)
- Noise distribution matches mechanism
- Budget is consumed correctly
- Repeated queries give different answers (due to randomness)

## Common Patterns

### Pattern 1: Numerical Observations

```json
{
  "path": "Observation.valueQuantity.value",
  "method": "differentialPrivacy",
  "epsilon": 0.1,
  "mechanism": "laplace",
  "sensitivity": 100.0,
  "description": "Assume values bounded [0, 100]"
}
```

### Pattern 2: Count Data

```json
{
  "path": "Encounter.period.start",
  "method": "differentialPrivacy",
  "epsilon": 0.2,
  "mechanism": "laplace",
  "sensitivity": 1.0,
  "description": "Counting encounters (sensitivity = 1)"
}
```

### Pattern 3: Combining with Other Methods

```json
[
  {
    "path": "Patient.identifier",
    "method": "cryptoHash",
    "comment": "Hash identifiers for linkage"
  },
  {
    "path": "Patient.birthDate",
    "method": "dateShift",
    "comment": "Shift dates deterministically"
  },
  {
    "path": "Observation.valueQuantity.value",
    "method": "differentialPrivacy",
    "epsilon": 0.1,
    "comment": "Add DP noise to measurements"
  }
]
```

## Limitations and Considerations

### When NOT to Use Differential Privacy

1. **Individual record release**: DP is for aggregate statistics, not individual records
2. **Deterministic linkage needed**: Noise breaks exact matching
3. **Zero privacy budget**: If ε exhausted, cannot answer more queries
4. **Very small datasets**: High noise relative to signal (n < 100)

### Utility Impact

**Low Impact** (ε ≥ 1.0):
- Aggregate queries (sums, counts, averages) remain accurate
- Statistical analysis (regression, correlation) mostly preserved
- Rare events may be distorted

**High Impact** (ε ≤ 0.1):
- Individual values significantly altered
- Rare categories may disappear
- Small group statistics unreliable
- Need larger datasets to compensate

### Security Considerations

1. **Cryptographically secure randomness**: Use System.Security.Cryptography.RandomNumberGenerator
2. **Side channels**: Ensure no information leaks through timing, memory, logs
3. **Budget exhaustion**: Reject queries once budget consumed
4. **Parameter validation**: Verify ε > 0, δ < 1/n, sensitivity > 0

## Integration with Existing Methods

### Method Compatibility

- ✅ **DP + cryptoHash**: Good for research with noisy measurements
- ✅ **DP + dateShift**: Compatible for different data types
- ✅ **DP + redact**: DP on numerical, redact on identifiers
- ✅ **DP + k-anonymity**: DP for sensitive attributes, k-anonymity for QIs
- ⚠️ **DP + encrypt**: Encryption should be applied after DP
- ❌ **DP + perturb**: Redundant (both add noise)

### Recommended Workflow

1. **Remove direct identifiers** (redact or cryptoHash)
2. **Apply k-anonymity to quasi-identifiers** (demographic data)
3. **Apply differential privacy to sensitive numerical data** (measurements, dates)
4. **Validate privacy properties** (k-anonymity validation, budget check)
5. **Test utility** (statistical queries, analysis workflows)

## Performance Considerations

### Computational Cost

- Laplace noise generation: Very fast (microseconds per value)
- Gaussian noise generation: Fast (microseconds per value)
- Budget tracking: Negligible overhead
- **Overall**: Minimal performance impact

### Memory Requirements

- Per-record processing (no batch required)
- Budget tracker: Single shared instance
- **Overall**: Very low memory footprint

## Regulatory Compliance

### HIPAA
- Differential privacy can satisfy Expert Determination method
- Must demonstrate re-identification risk is very small
- Document epsilon choice and justification

### GDPR
- Differential privacy is a recognized pseudonymization technique
- Satisfies "appropriate technical measures" for data protection
- May still require legal basis for processing

### FDA/Clinical Trials
- Acceptable for secondary analysis of trial data
- Must not compromise primary endpoints
- Document in Data Management Plan

## Advanced Topics

### Advanced Composition

For complex analyses, use optimal composition theorem:
- Standard composition: ε_total = Σ ε_i
- Advanced composition: ε_total ≈ √(2k ln(1/δ)) × ε + k × ε × e^ε
  where k = number of queries
- Can save significant budget for many queries

### Local vs Global Differential Privacy

**Global DP** (implemented here):
- Trusted curator adds noise to aggregate results
- Better utility
- Requires trust in data holder

**Local DP** (future work):
- Each individual adds noise to their own data before submission
- No trusted curator needed
- Worse utility (more noise required)

## Troubleshooting

### Problem: Too much noise, results unusable

**Solutions**:
- Increase epsilon (reduce privacy)
- Increase dataset size (noise is relative to signal)
- Use Gaussian instead of Laplace mechanism
- Reduce sensitivity by better bounding data
- Consider aggregating data before DP (if appropriate)

### Problem: Budget exhausted too quickly

**Solutions**:
- Increase total budget (reduce privacy)
- Use parallel composition where possible (disjoint data subsets)
- Implement advanced composition
- Prioritize most important queries
- Use sampling (answer fewer queries)

### Problem: Validation fails (privacy property not met)

**Solutions**:
- Verify noise is actually being added (check logs)
- Check random number generator is properly initialized
- Ensure epsilon/delta parameters are correctly loaded
- Verify sensitivity calculation is correct

## References

- Dwork, C. (2006). "Differential Privacy." International Colloquium on Automata, Languages, and Programming.
- Dwork, C., & Roth, A. (2014). "The Algorithmic Foundations of Differential Privacy." Foundations and Trends in Theoretical Computer Science.
- Apple's Differential Privacy Team (2017). "Learning with Privacy at Scale."
- Google's Private data protection guide: https://developers.google.com/privacy

## See Also

- [K-Anonymity Configuration](k-anonymity-configuration.md)
- [Privacy-Utility Tradeoffs](privacy-utility-tradeoffs.md)
- [FHIR Anonymization Overview](FHIR-anonymization.md)
- [GDPR Article 89 Configuration](GDPR-Article89-configuration.md)
