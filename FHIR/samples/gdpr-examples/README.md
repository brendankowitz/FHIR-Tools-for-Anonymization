# GDPR Article 89 Anonymization Examples

This directory contains example scenarios demonstrating the use of the GDPR Article 89 anonymization configuration for scientific research purposes.

## Overview

The GDPR Article 89 configuration (`configuration-gdpr-article89.json`) implements privacy-preserving measures aligned with EU data protection requirements for scientific research, including:

- **Pseudonymization**: Identifiers are replaced with cryptographic hashes to maintain research utility while protecting individual privacy
- **Strict redaction**: Direct identifiers (names, addresses, detailed locations) are removed
- **Date shifting**: Temporal relationships are preserved while obscuring absolute dates
- **Special category data protection**: Enhanced protection for genetic, biometric, and sensitive health data
- **Data minimization**: Only data necessary for research purposes is retained

## Example Use Cases

### 1. Clinical Trial Data Anonymization

**Scenario**: A pharmaceutical company conducting a multi-site clinical trial needs to share patient data with research partners while complying with GDPR Article 89.

**Requirements**:
- Maintain patient linkage across resources (using pseudonymized IDs)
- Preserve temporal relationships for treatment timelines
- Protect direct identifiers
- Enable longitudinal analysis

**Configuration approach**:
```bash
# Use the GDPR Article 89 configuration with a consistent crypto hash key
./Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool \
  -i ./clinical-trial-data \
  -o ./anonymized-output \
  -c configuration-gdpr-article89.json
```

**What gets anonymized**:
- Patient identifiers → Pseudonymized with cryptoHash (consistent across resources)
- Names → Redacted
- Addresses → Redacted (except country for regional analysis)
- Contact details → Redacted
- Dates → Shifted by consistent offset per patient
- Medical Record Numbers → Pseudonymized
- Device identifiers → Pseudonymized
- Photos/biometric data → Redacted

**What is preserved**:
- Clinical observations and measurements
- Medication records (with anonymized references)
- Diagnostic results
- Temporal relationships between events
- Age (generalized if over 89 years)
- Country (for geographic analysis)

### 2. Epidemiological Study

**Scenario**: A public health agency analyzes disease patterns across regions to study outbreak trends and risk factors.

**Requirements**:
- Geographic aggregation at country level
- Temporal pattern analysis
- No individual re-identification risk
- Compliance with GDPR special category data rules

**Configuration approach**:
```bash
# Use GDPR configuration with date shifting for temporal analysis
./Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool \
  -i ./surveillance-data \
  -o ./research-dataset \
  -c configuration-gdpr-article89.json \
  -b  # bulk data format
```

**Analytical capabilities maintained**:
- Disease prevalence by country
- Temporal trends (dates shifted but relationships preserved)
- Age-based risk analysis
- Comorbidity patterns
- Treatment outcomes

### 3. Health Services Research

**Scenario**: Researchers analyze healthcare utilization patterns and treatment pathways across a hospital network.

**Requirements**:
- Link patient encounters and procedures
- Analyze resource utilization
- Study care coordination patterns
- Remove all direct identifiers
- Maintain organizational context

**Configuration approach**:
```bash
# GDPR configuration preserves organizational identifiers for context
./Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool \
  -i ./hospital-encounters \
  -o ./research-output \
  -c configuration-gdpr-article89.json \
  -r  # recursive processing
```

**Research capabilities**:
- Patient journey analysis (via pseudonymized IDs)
- Treatment pathway patterns
- Service utilization metrics
- Length of stay analysis
- Referral patterns
- Cost analysis (if included in data)

### 4. Genomic Research

**Scenario**: A research consortium studies genetic variants associated with disease outcomes.

**Requirements**:
- Maximum protection for genetic data (special category under GDPR)
- Remove all direct identifiers
- Maintain clinical phenotype data
- Enable variant-phenotype association studies

**Configuration approach**:
```bash
# GDPR configuration applies strict rules to genetic/biometric data
./Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool \
  -i ./genomic-study-data \
  -o ./anonymized-genomic-data \
  -c configuration-gdpr-article89.json
```

**Special protections applied**:
- Genetic sequences → Redacted (unless specifically needed and additional safeguards applied)
- Biometric identifiers → Redacted
- Patient identifiers → Pseudonymized
- Detailed locations → Redacted
- Combined with clinical data → Only via pseudonymized linkage

## Testing the Configuration

Before using the GDPR configuration on real data, test with sample data:

```bash
# Test with sample R4 files
./Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool \
  -i ../fhir-r4-files \
  -o ./test-output \
  -c configuration-gdpr-article89.json \
  -v  # verbose output

# Validate the anonymized output
./Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool \
  -i ../fhir-r4-files \
  -o ./test-output \
  -c configuration-gdpr-article89.json \
  --validateInput \
  --validateOutput
```

## Important Considerations

### 1. Purpose Limitation
GDPR Article 89 allows processing for research purposes **only**. Ensure your data processing agreement and consent basis explicitly permit research use.

### 2. Appropriate Safeguards
The configuration implements technical measures (pseudonymization, redaction, date shifting). Additional organizational safeguards are required:
- Access controls
- Audit logging
- Data processing agreements
- Security measures (encryption in transit/at rest)
- Researcher training on data handling

### 3. Legal Basis
Consult with your Data Protection Officer (DPO) and legal team to ensure:
- Appropriate legal basis (consent, public interest, legitimate interest)
- Compliance with national implementations of GDPR
- Fulfillment of transparency obligations
- Data subject rights management

### 4. Data Minimization
The configuration provides baseline anonymization. Consider:
- Removing additional fields not needed for your specific research question
- Further aggregating geographic data if country-level is too granular
- Applying additional generalizations based on your risk assessment

### 5. Re-identification Risk
Even with anonymization, re-identification risk may exist with:
- Small populations
- Rare conditions
- Combinations of quasi-identifiers
- Linkage with external datasets

Conduct a Data Protection Impact Assessment (DPIA) for high-risk processing.

## Configuration Parameters

The GDPR Article 89 configuration uses these key parameters:

```json
"parameters": {
  "dateShiftKey": "your-secure-key",
  "dateShiftScope": "resource",
  "dateShiftRange": 100,
  "cryptoHashKey": "your-secure-key",
  "encryptKey": "your-secure-key"
}
```

**Security requirements**:
- Use strong, randomly generated keys (minimum 32 characters)
- Store keys securely (e.g., Azure Key Vault, AWS Secrets Manager)
- Never commit keys to version control
- Rotate keys according to your security policy
- Use consistent keys for datasets that need to be linked

## Compliance Documentation

For each anonymization project, maintain documentation of:

1. **Purpose**: Specific research question and legal basis
2. **Configuration**: Version of configuration file used
3. **Parameters**: Key identifiers (not values), date shift range
4. **Data flow**: Input sources, processing steps, output destinations
5. **Risk assessment**: DPIA outcomes and mitigation measures
6. **Access controls**: Who can access the anonymized data
7. **Retention**: How long data will be retained
8. **Deletion**: Procedures for secure deletion when no longer needed

## Further Resources

- [GDPR Article 89 Configuration Documentation](../../../docs/GDPR-Article89-configuration.md)
- [GDPR Compliance Mapping](../../../docs/GDPR-compliance-mapping.md)
- [FHIR Anonymization Documentation](../../../docs/FHIR-anonymization.md)
- [EDPB Guidelines on Research](https://edpb.europa.eu/)
- [ICO Guidance on Anonymisation](https://ico.org.uk/for-organisations/guide-to-data-protection/guide-to-the-general-data-protection-regulation-gdpr/lawful-basis-for-processing/consent/)

## Support and Questions

This configuration is provided as a reference implementation. Organizations remain responsible for:
- Assessing suitability for their specific use case
- Obtaining appropriate legal advice
- Conducting risk assessments
- Implementing additional safeguards as needed
- Maintaining compliance with GDPR and national laws

For technical questions about the anonymization tool, please refer to the main [FHIR anonymization documentation](../../../docs/FHIR-anonymization.md) or open an issue in the repository.

---

**Disclaimer**: This configuration and examples are provided for informational purposes. They do not constitute legal advice. Consult with your Data Protection Officer and legal counsel to ensure compliance with GDPR and applicable regulations.
