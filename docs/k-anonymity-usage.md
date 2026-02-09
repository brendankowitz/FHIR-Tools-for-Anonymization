# K-Anonymity Configuration Guide

## Overview

This document explains how to use k-anonymity transformations in FHIR data anonymization. 

**⚠️ IMPORTANT LIMITATION**: The k-anonymity processor provides generalization and suppression transformations commonly used for k-anonymity, but **does NOT guarantee k-anonymity by itself**.

True k-anonymity is a dataset-level property that requires:
1. **Batch processing** across all records
2. **Equivalence class formation** (grouping records with identical quasi-identifiers)
3. **Validation** that each group has ≥ k members
4. **Suppression or re-generalization** of records in small groups

## What the K-Anonymity Processor Does

The processor applies generalization transformations to individual FHIR elements:

- **Range generalization**: Numeric values → ranges (age 42 → "40-49")
- **Hierarchy generalization**: Structured values → less specific (zip 98052 → "980**")
- **Suppression**: Complete removal when configured

These are **necessary but not sufficient** for k-anonymity.

## Configuration

### Method: `kAnonymity`

**Parameters:**

| Parameter | Required | Type | Description |
|-----------|----------|------|-------------|
| `kValue` | Yes | integer | Target k value (minimum group size). Must be ≥ 2. Used for documentation and validation, not enforced during processing. |
| `generalizationStrategy` | Yes | string | How to generalize: `"range"`, `"hierarchy"`, or `"suppression"` |
| `suppressionStrategy` | No | string | How to suppress if strategy=suppression: `"redact"` or `"remove"` (default: "redact") |

### Example Configuration

```json
{
  "fhirVersion": "R4",
  "fhirPathRules": [
    {
      "path": "Patient.address.postalCode",
      "method": "kAnonymity",
      "kValue": 5,
      "generalizationStrategy": "hierarchy"
    },
    {
      "path": "Patient.extension.where(url='http://example.org/age').valueInteger",
      "method": "kAnonymity",
      "kValue": 5,
      "generalizationStrategy": "range"
    }
  ]
}
```

## Generalization Strategies

### Range Generalization (`"range"`)

**Best for**: Numeric values (ages, counts, measurements)

**Behavior**:
- Integers → Age ranges (0-9, 10-19, 20-29, ..., 90+)
- Decimals → Rounded to nearest 10
- Strings → First 3 characters + "**"

**Example**:
```
42 → "40-49"
156.7 → "160"
"98052" → "980**"
```

### Hierarchy Generalization (`"hierarchy"`)

**Best for**: Coded values, addresses, identifiers with natural hierarchies

**Behavior**:
- Postal/zip codes → First 3 digits + "**" (98052 → "980**")
- Multi-word strings → First word only ("Seattle WA" → "Seattle")
- Other strings → First character + asterisks ("Smith" → "S***")

**Example**:
```
"98052" → "980**"
"New York" → "New"
"Johnson" → "J***"
```

### Suppression (`"suppression"`)

**Best for**: Highly identifying values that cannot be safely generalized

**Behavior**:
- Removes value entirely (sets to null)
- Most aggressive privacy protection
- Maximum utility loss

## Achieving True K-Anonymity

To achieve actual k-anonymity guarantees:

### Step 1: Identify Quasi-Identifiers

Quasi-identifiers are attributes that, in combination, could re-identify individuals:
- Age or birth year
- Gender
- Geographic location (zip code, city)
- Occupation
- Diagnosis codes
- Visit dates

**NOT quasi-identifiers** (already strongly protected):
- Direct identifiers (names, SSN, MRN) → use `redact` or `cryptoHash`
- Dates → use `dateShift`

### Step 2: Configure Generalization

Apply k-anonymity method to ALL quasi-identifiers:

```json
{
  "fhirPathRules": [
    {
      "path": "Patient.address.postalCode",
      "method": "kAnonymity",
      "kValue": 5,
      "generalizationStrategy": "hierarchy"
    },
    {
      "path": "Patient.extension.where(url='http://example.org/age').valueInteger",
      "method": "kAnonymity",
      "kValue": 5,
      "generalizationStrategy": "range"
    },
    {
      "path": "Patient.gender",
      "method": "keep"
    }
  ]
}
```

### Step 3: Anonymize Dataset

```bash
dotnet Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool.dll \
  -c configuration-k-anonymity.json \
  -i input-data/ \
  -o anonymized-data/
```

### Step 4: Validate K-Anonymity Property

**Use the validation tools** (see [K-Anonymity Validation](./k-anonymity-validation.md)):

```csharp
using Microsoft.Health.Fhir.Anonymizer.Core.Validation;

// Extract quasi-identifiers from anonymized dataset
var equivalenceClasses = BuildEquivalenceClasses(
    anonymizedRecords, 
    quasiIdentifiers: new[] { "age", "gender", "zipCode" }
);

// Validate k-anonymity
var validator = new KAnonymityValidator();
var result = validator.Validate(equivalenceClasses, kValue: 5);

if (!result.IsValid)
{
    Console.WriteLine($"K-anonymity violated! {result.Violations.Count} groups below k=5");
    // Option 1: Further generalize small groups
    // Option 2: Suppress records in small groups
    // Option 3: Lower k value and re-validate
}
```

### Step 5: Assess Re-identification Risk

```csharp
using Microsoft.Health.Fhir.Anonymizer.Core.Validation;

var assessor = new ReidentificationRiskAssessor();
var riskReport = assessor.AssessRisk(equivalenceClasses);

Console.WriteLine($"Prosecutor Risk (max): {riskReport.ProsecutorRisk:P2}");
Console.WriteLine($"Journalist Risk (avg): {riskReport.JournalistRisk:P2}");
Console.WriteLine($"Risk Level: {riskReport.RiskLevel}"); // Low, Medium, High
```

## Selecting K Value

| k Value | Privacy Protection | Data Utility | Use Case |
|---------|-------------------|--------------|----------|
| k=2 | Minimal | High | Internal analytics, low risk |
| k=5 | Moderate | Good | Research, published datasets |
| k=10 | Strong | Moderate | Public health, external sharing |
| k=20+ | Very Strong | Lower | High-risk data, strict compliance |

**Tradeoff**: Higher k → better privacy but more generalization/suppression → lower utility

**Guidelines**:
- **Small datasets** (<1000 records): k=3-5 (higher k may suppress too much)
- **Medium datasets** (1000-100,000): k=5-10
- **Large datasets** (>100,000): k=10-20
- **High re-identification risk**: Start with k=10, validate, increase if needed

## Common Pitfalls

### ❌ Assuming Processing = K-Anonymity

**Problem**: Running the k-anonymity processor and assuming the output is k-anonymous.

**Solution**: Always validate with `KAnonymityValidator` after anonymization.

### ❌ Missing Quasi-Identifiers

**Problem**: Generalizing age and gender but leaving zip code unchanged.

**Solution**: Apply generalization to **all** quasi-identifiers consistently.

### ❌ K Value Too High for Dataset Size

**Problem**: Setting k=20 on a 100-record dataset results in >80% suppression.

**Solution**: Choose k proportional to dataset size. Validate suppression rate.

### ❌ Inconsistent Generalization Levels

**Problem**: Some records generalized to "40-49", others kept exact age "42".

**Solution**: Use consistent FHIRPath rules and validate equivalence classes.

## Integration with Other Methods

### K-Anonymity + Crypto Hash (Recommended)

```json
{
  "path": "Patient.identifier",
  "method": "cryptoHash",
  "hashFunction": "SHA256",
  "key": "your-secret-key"
},
{
  "path": "Patient.address.postalCode",
  "method": "kAnonymity",
  "kValue": 5,
  "generalizationStrategy": "hierarchy"
}
```

**Why**: Direct identifiers get strong pseudonymization, quasi-identifiers get generalized.

### K-Anonymity + Date Shift

```json
{
  "path": "Patient.birthDate",
  "method": "dateShift",
  "span": 50
},
{
  "path": "Patient.extension.where(url='http://example.org/age').valueInteger",
  "method": "kAnonymity",
  "kValue": 5,
  "generalizationStrategy": "range"
}
```

**Why**: Dates get temporal protection, derived age gets generalized for k-anonymity.

## See Also

- [K-Anonymity Validation](./k-anonymity-validation.md) - Using validation tools
- [Privacy-Utility Tradeoffs](./privacy-utility-tradeoffs.md) - Balancing privacy and data quality
- [Differential Privacy](./differential-privacy-configuration.md) - Alternative approach
- [HIPAA Safe Harbor](./FHIR-anonymization.md#hipaa-safe-harbor) - Compliance comparison
