# K-Anonymity Configuration Guide

## Overview

K-anonymity is a privacy model that ensures each record is indistinguishable from at least k-1 other records with respect to certain identifying attributes (quasi-identifiers). This protection method reduces re-identification risk by creating equivalence classes where each group contains at least k records.

## Important Limitation

⚠️ **Critical**: The current k-anonymity processor provides **generalization transformations** for individual records but does **NOT guarantee k-anonymity** without batch validation. 

To achieve true k-anonymity:
1. Apply k-anonymity transformations during anonymization
2. Use `KAnonymityValidator` to verify the property holds across the entire dataset
3. If validation fails, adjust parameters (increase generalization, reduce k-value) and re-process

See [K-Anonymity Usage Guide](k-anonymity-usage.md) for the complete workflow.

## Theory

K-anonymity protects against re-identification by ensuring that released information cannot be distinguished on fewer than k records. The value of k represents the minimum group size for equivalent records.

### Quasi-Identifiers

Quasi-identifiers (QIs) are attributes that, when combined, can potentially identify an individual:
- Age or birth date
- Gender
- Geographic location (zip code, city, state)
- Race/ethnicity
- Occupation
- Visit dates

### Generalization Strategies

To achieve k-anonymity, quasi-identifiers are generalized:

1. **Range-based**: Convert precise values to ranges
   - Age 34 → Age range 30-35
   - Date 2023-05-15 → Year 2023, Month May

2. **Hierarchy-based**: Use taxonomic hierarchies
   - Zip code 98101 → Zip prefix 981** → State WA
   - City Seattle → State WA → Region Pacific Northwest

3. **Suppression**: Remove values that prevent achieving k-anonymity
   - Outliers or rare combinations
   - Records that cannot be grouped

## Configuration Parameters

### Basic Configuration

```json
{
  "path": "Patient.birthDate",
  "method": "kAnonymity",
  "quasiIdentifiers": [
    "Patient.birthDate",
    "Patient.address.postalCode",
    "Patient.gender"
  ],
  "kValue": 5,
  "generalizationStrategy": "ageRange",
  "ageRangeYears": 5
}
```

### Parameters

- **`quasiIdentifiers`** (required): Array of FHIR paths that form the set of quasi-identifiers
  - All paths in this set must use the same quasi-identifier list
  - Defines the equivalence classes for grouping

- **`kValue`** (required): Minimum group size (k ≥ 2)
  - Higher values = stronger privacy, lower data utility
  - Typical values: 5-10 for moderate privacy, 10-100 for high privacy
  - Must consider dataset size (k too large = high suppression)

- **`generalizationStrategy`** (required): How to generalize this field
  - `"ageRange"`: Convert birth dates to age ranges
  - `"hierarchy"`: Use hierarchical suppression (zip codes)
  - `"retain"`: Keep as-is (for categorical QIs like gender)
  - `"suppress"`: Remove value if k-anonymity violated

- **`ageRangeYears`** (for ageRange strategy): Size of age brackets in years
  - Default: 5 years
  - Common values: 5, 10, or 20 years

- **`hierarchyLevel`** (for hierarchy strategy): Level in hierarchy to generalize to
  - Level 0: Full precision (98101)
  - Level 3: 3-digit prefix (981**)
  - Level 5: State level (WA)

## Complete Example

### Patient Demographics with K=5

```json
{
  "fhirVersion": "R4",
  "processingErrors": "raise",
  "fhirPathRules": [
    {
      "path": "Patient.birthDate",
      "method": "kAnonymity",
      "quasiIdentifiers": [
        "Patient.birthDate",
        "Patient.address.postalCode",
        "Patient.gender"
      ],
      "kValue": 5,
      "generalizationStrategy": "ageRange",
      "ageRangeYears": 5
    },
    {
      "path": "Patient.address.postalCode",
      "method": "kAnonymity",
      "quasiIdentifiers": [
        "Patient.birthDate",
        "Patient.address.postalCode",
        "Patient.gender"
      ],
      "kValue": 5,
      "generalizationStrategy": "hierarchy",
      "hierarchyLevel": 3
    },
    {
      "path": "Patient.gender",
      "method": "kAnonymity",
      "quasiIdentifiers": [
        "Patient.birthDate",
        "Patient.address.postalCode",
        "Patient.gender"
      ],
      "kValue": 5,
      "generalizationStrategy": "retain"
    }
  ]
}
```

## Selecting Quasi-Identifiers

### Guidelines

1. **Include all potentially identifying attributes**: Start broad and test
2. **Consider linkage attacks**: Attributes that could be linked to external datasets
3. **Balance privacy and utility**: More QIs = stronger privacy but more generalization

### Common FHIR Quasi-Identifiers

**Patient Resource:**
- `Patient.birthDate`
- `Patient.gender`
- `Patient.address.postalCode`
- `Patient.address.city`
- `Patient.address.state`
- `Patient.maritalStatus`

**Clinical Data:**
- `Encounter.period.start` (visit date)
- `Condition.onsetDateTime` (diagnosis date)
- `Procedure.performedDateTime`
- `Observation.effectiveDateTime`

### Risk Assessment

**High Re-identification Risk** (always include as QIs):
- Birth date + zip code + gender ("87% uniqueness" - Sweeney, 2000)
- Full dates of rare events
- Geographic data more precise than city level

**Moderate Risk**:
- Diagnosis codes (common vs rare)
- Medication codes
- Procedure codes

**Low Risk** (may not need to be QIs):
- Common lab test types
- General categories (gender, race)

## Choosing K-Value

### Guidelines

| K-Value | Privacy Level | Use Case | Suppression Risk |
|---------|---------------|----------|------------------|
| 2-3 | Minimal | Internal research, large datasets | Low |
| 5-10 | Moderate | Common research, HIPAA-aligned | Moderate |
| 10-50 | High | Public release, sensitive data | High |
| 50-100 | Very High | Highly sensitive, regulatory | Very High |

### Considerations

1. **Dataset Size**: Larger datasets can support higher k-values
   - Small dataset (< 1,000 records): k ≤ 5
   - Medium dataset (1,000-10,000): k = 5-10
   - Large dataset (> 10,000): k = 10-100

2. **Data Distribution**: Sparse data requires lower k-values
   - Many unique combinations → increase generalization
   - Homogeneous data → can use higher k

3. **Regulatory Requirements**:
   - HIPAA Safe Harbor: k ≥ 5 often sufficient
   - GDPR: Risk-based; may need k ≥ 10 for sensitive data
   - Clinical trials: Varies by protocol and IRB

## Validation

After anonymization, validate k-anonymity property:

```csharp
var validator = new KAnonymityValidator();
var result = validator.Validate(anonymizedData, quasiIdentifiers, kValue);

if (!result.IsKAnonymous)
{
    Console.WriteLine($"K-anonymity violated!");
    Console.WriteLine($"Groups with size < {kValue}: {result.ViolationCount}");
    Console.WriteLine($"Smallest group size: {result.MinGroupSize}");
}
```

## Common Issues and Solutions

### Issue: High Suppression Rate (>30%)

**Solutions:**
- Reduce k-value
- Increase generalization (larger age ranges, higher hierarchy levels)
- Remove quasi-identifiers that create many unique combinations
- Consider using differential privacy for highly unique attributes

### Issue: K-Anonymity Validation Fails

**Solutions:**
- Verify all QI fields use the same `quasiIdentifiers` list in configuration
- Check for missing values (NULLs can break equivalence classes)
- Ensure generalization strategies are consistent
- Use batch processing mode (not single-record mode)

### Issue: Utility Loss Too High

**Solutions:**
- Use smaller age ranges (ageRangeYears: 5 instead of 10)
- Reduce hierarchy level (3-digit zip instead of state-level)
- Combine with other methods: k-anonymity for QIs + cryptoHash for identifiers
- Consider l-diversity or t-closeness for sensitive attributes

## Integration with Other Methods

### Recommended Combinations

```json
[
  {
    "path": "Patient.identifier",
    "method": "cryptoHash",
    "comment": "Hash identifiers for pseudonymization"
  },
  {
    "path": "Patient.birthDate",
    "method": "kAnonymity",
    "comment": "Generalize quasi-identifiers"
  },
  {
    "path": "Patient.name",
    "method": "redact",
    "comment": "Remove direct identifiers"
  },
  {
    "path": "Observation.valueQuantity.value",
    "method": "differentialPrivacy",
    "comment": "Add noise to sensitive numerical data"
  }
]
```

### Method Compatibility

- ✅ **K-anonymity + cryptoHash**: Excellent for research with re-linkage
- ✅ **K-anonymity + dateShift**: Good for temporal analysis
- ✅ **K-anonymity + differential privacy**: Strong protection for aggregate queries
- ⚠️ **K-anonymity + generalize**: Redundant; k-anonymity includes generalization
- ❌ **K-anonymity + encrypt**: Incompatible; encryption prevents validation

## Performance Considerations

### Memory Requirements

K-anonymity requires batch processing:
- Must load all records to build equivalence classes
- Memory usage: O(n * m) where n = records, m = QI fields
- Recommendation: Process in chunks for very large datasets (>1M records)

### Processing Time

- Single-pass generalization: Fast (ms per record)
- Equivalence class formation: O(n log n)
- Validation: O(n)
- Total: Acceptable for most datasets (<1min for 100k records)

## References

- Sweeney, L. (2002). "k-anonymity: A model for protecting privacy." International Journal of Uncertainty, Fuzziness and Knowledge-Based Systems.
- El Emam, K., et al. (2011). "A systematic review of re-identification attacks on health data." PLoS ONE.
- HIPAA Privacy Rule: https://www.hhs.gov/hipaa/for-professionals/privacy/index.html

## See Also

- [K-Anonymity Usage Guide](k-anonymity-usage.md) - Complete workflow for achieving true k-anonymity
- [Differential Privacy Configuration](differential-privacy-configuration.md)
- [Privacy-Utility Tradeoffs](privacy-utility-tradeoffs.md)
- [FHIR Anonymization Overview](FHIR-anonymization.md)
