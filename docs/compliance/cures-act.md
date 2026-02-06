# 21st Century Cures Act and Information Blocking Compliance Guide

## Executive Summary

The 21st Century Cures Act Final Rule (45 CFR Part 171) establishes regulations to prevent information blocking and promote health data interoperability. Healthcare organizations must balance their obligations to share data with their responsibilities to protect patient privacy under HIPAA.

**Tools for Health Data Anonymization** enables organizations to safely share healthcare data for permitted purposes while maintaining compliance with both the Cures Act's data sharing mandates and HIPAA's privacy requirements. By anonymizing FHIR and DICOM data, organizations can leverage the **Privacy Exception** to information blocking regulations, enabling research, public health reporting, and quality improvement without exposing protected health information (PHI).

---

## Cures Act Overview

### Purpose and Goals

The 21st Century Cures Act, enacted in December 2016, aims to:

- **Promote Interoperability**: Enable seamless exchange of electronic health information (EHI) across systems and organizations
- **Empower Patients**: Give patients and their authorized representatives timely access to their health information
- **Reduce Information Blocking**: Prohibit practices that interfere with the access, exchange, or use of EHI
- **Support Innovation**: Enable secondary use of health data for research, public health, and quality improvement

### Key Definitions

**Electronic Health Information (EHI)**: The Cures Act defines EHI broadly as electronic protected health information (ePHI) to the extent that it would be included in a designated record set, regardless of whether the group or entity is covered by or subject to HIPAA.

**Information Blocking**: Practices by health IT developers, health information networks, health information exchanges, or healthcare providers that are likely to interfere with, prevent, or materially discourage access, exchange, or use of EHI.

---

## Information Blocking Regulations (45 CFR § 171.201-203)

### Who is Subject to Information Blocking Regulations?

1. **Healthcare Providers** (as defined in 42 U.S.C. § 300jj(3))
2. **Health Information Exchanges (HIEs)**
3. **Health Information Networks (HINs)**
4. **Health IT Developers** of certified health IT

### Prohibition on Information Blocking

The regulations prohibit practices that:
- Are likely to interfere with, prevent, or materially discourage access, exchange, or use of EHI
- Do not fall within one of the eight regulatory exceptions

### The Eight Exceptions to Information Blocking

Organizations may limit information sharing when practices meet specific conditions under these exceptions:

1. **Preventing Harm Exception** (§ 171.201) - To protect patient safety or the integrity of EHI
2. **Privacy Exception** (§ 171.202) - To protect an individual's privacy consistent with applicable law
3. **Security Exception** (§ 171.203) - To protect the security of EHI
4. **Infeasibility Exception** (§ 171.204) - When information sharing is technically infeasible
5. **Health IT Performance Exception** (§ 171.205) - For maintenance or improvements to health IT
6. **Content and Manner Exception** (§ 171.301) - Regarding how information is provided
7. **Fees Exception** (§ 171.302) - Regarding permissible costs for providing information
8. **Licensing Exception** (§ 171.303) - When licensing terms prevent sharing

---

## Privacy Exception and Anonymization (45 CFR § 171.202)

### Understanding the Privacy Exception

The **Privacy Exception** permits organizations to limit access, exchange, or use of EHI to protect patient privacy, provided the practice:

1. **Is consistent with applicable law**: Must comply with HIPAA Privacy Rule (45 CFR Part 160 and Part 164, Subparts A and E)
2. **Is non-discriminatory**: Cannot be applied in a manner that discriminates
3. **Is implemented in good faith**: Genuinely protects privacy rather than impeding information flow

### How Anonymization Enables Compliant Data Sharing

**De-identification under HIPAA** removes the privacy protection requirements of the HIPAA Privacy Rule. Once data is properly de-identified, it is no longer considered PHI and can be shared more freely for:

- **Research studies** without individual authorization
- **Public health surveillance** and population health analytics
- **Quality improvement** initiatives and benchmarking
- **Data sharing** with third-party analytics platforms
- **Training and development** of AI/ML models

### Two Paths to De-identification

The HIPAA Privacy Rule (45 CFR § 164.514) provides two methods for de-identification:

#### 1. Safe Harbor Method (§ 164.514(b)(2))

Remove the following 18 identifiers (plus any other unique identifying information):

1. Names
2. Geographic subdivisions smaller than a state (except first 3 digits of ZIP if > 20,000 people)
3. Dates (except year) directly related to an individual, including birth date, admission date, discharge date, date of death
4. Telephone numbers
5. Fax numbers
6. Email addresses
7. Social Security numbers
8. Medical record numbers
9. Health plan beneficiary numbers
10. Account numbers
11. Certificate/license numbers
12. Vehicle identifiers and serial numbers, including license plate numbers
13. Device identifiers and serial numbers
14. Web Universal Resource Locators (URLs)
15. Internet Protocol (IP) addresses
16. Biometric identifiers, including finger and voice prints
17. Full-face photographs and comparable images
18. Any other unique identifying number, characteristic, or code

**This tool's default configuration** is based on the Safe Harbor method, providing a compliant starting point for most use cases.

#### 2. Expert Determination Method (§ 164.514(b)(1))

A qualified expert applies statistical or scientific principles to determine that the risk of re-identification is very small. This method allows organizations to retain more utility in data while maintaining privacy protection.

---

## HIPAA Safe Harbor Mapping to Tool Capabilities

### How This Tool Implements Safe Harbor

The Tools for Health Data Anonymization provides multiple anonymization methods that can be configured to meet Safe Harbor requirements:

| Safe Harbor Identifier | FHIR/DICOM Element Examples | Anonymization Method |
|------------------------|----------------------------|---------------------|
| Names | Patient.name, Practitioner.name | `redact`, `substitute` |
| Geographic subdivisions | Address.postalCode (keep first 3 digits only) | `generalize` |
| Dates | dateTime elements, birthDate | `dateShift`, `redact`, `generalize` (to year) |
| Phone/Fax/Email | ContactPoint values | `redact`, `cryptoHash` |
| SSN, MRN, Account Numbers | Identifier values | `redact`, `cryptoHash`, `encrypt` |
| URLs, IPs | References, endpoints | `redact`, `cryptoHash` |
| Biometrics, Photos | Observation values, images | `redact`, `perturb` |

### Configuration-Driven Compliance

Organizations can customize anonymization rules through configuration files:

```json
{
  "fhirPathRules": [
    {
      "path": "Patient.name",
      "method": "redact"
    },
    {
      "path": "Patient.birthDate",
      "method": "dateShift",
      "dateShiftKey": "Patient.id",
      "dateShiftScope": "resource"
    }
  ]
}
```

See [FHIR Anonymization Documentation](../FHIR-anonymization.md) for complete configuration guidance.

---

## Use Cases: Cures Act Compliance Through Anonymization

### 1. Research Data Sharing

**Scenario**: A health system wants to share EHI with academic researchers studying cardiovascular disease outcomes.

**Cures Act Requirement**: Must provide access to EHI unless an exception applies.

**Privacy Challenge**: Sharing identifiable data requires individual authorization under HIPAA, which may be impractical for large cohorts or retrospective studies.

**Solution with Anonymization**:
- Apply Safe Harbor de-identification to the dataset
- Share de-identified data freely without individual authorization
- Maintain compliance with both Cures Act (data shared) and HIPAA (privacy protected)

**Tool Configuration**: Use default Safe Harbor configuration with date shifting to preserve temporal relationships.

---

### 2. Public Health Surveillance

**Scenario**: A state public health agency requests population-level data on vaccination rates and chronic disease prevalence from multiple healthcare organizations.

**Cures Act Requirement**: Public health reporting is a permitted purpose; organizations should facilitate data sharing.

**Privacy Challenge**: While public health reporting is permitted under HIPAA, organizations may want additional privacy protection for sensitive conditions.

**Solution with Anonymization**:
- Aggregate data at appropriate geographic levels (county, state)
- Apply anonymization to remove direct identifiers
- Retain statistical utility for epidemiological analysis
- Enable cross-organizational data pooling without re-identification risk

**Tool Configuration**: Use `generalize` method for geographic data, `redact` for direct identifiers, retain aggregate counts.

---

### 3. Quality Improvement and Benchmarking

**Scenario**: A hospital network wants to compare quality metrics across facilities and benchmark against national standards.

**Cures Act Requirement**: Healthcare providers should support quality improvement activities.

**Privacy Challenge**: Individual patient data should not be identifiable in quality reports, especially when shared with external benchmarking organizations.

**Solution with Anonymization**:
- De-identify patient-level data before aggregation
- Apply consistent anonymization across facilities
- Enable valid statistical comparisons while protecting privacy

**Tool Configuration**: Use `dateShift` with consistent keys for temporal analysis, `cryptoHash` for linking records within the de-identified dataset.

---

### 4. Patient Access with Privacy Protection

**Scenario**: A patient requests their complete medical record in electronic format, but the record contains sensitive notes about family members.

**Cures Act Requirement**: Provide timely access to the patient's EHI.

**Privacy Challenge**: Must protect privacy of third parties mentioned in the record.

**Solution with Anonymization**:
- Provide complete patient record
- Apply targeted redaction to third-party names and identifiers
- Maintain completeness of the patient's own information

**Tool Configuration**: Use FHIRPath rules to selectively redact third-party information in DocumentReference, Observation, and ClinicalImpression resources.

---

### 5. AI/ML Model Training

**Scenario**: A health IT developer wants to train machine learning models on real-world clinical data to improve clinical decision support algorithms.

**Cures Act Requirement**: Innovation and secondary use of health data is encouraged.

**Privacy Challenge**: Training data must not expose patient PHI, especially when models may be deployed broadly.

**Solution with Anonymization**:
- Create de-identified training datasets using Safe Harbor method
- Apply date shifting to preserve temporal patterns
- Use cryptographic hashing for consistent patient linkage within the dataset
- Enable model training without privacy violations

**Tool Configuration**: Comprehensive anonymization with `dateShift` for temporal relationships, `cryptoHash` for pseudo-identifiers, `perturb` for numeric values if differential privacy is needed.

---

## Compliance Checklist for Organizations

Organizations implementing these tools should follow this checklist to ensure compliance with both the Cures Act and HIPAA:

### Assessment Phase

- [ ] **Identify the purpose** of data sharing (research, public health, quality improvement, patient access, etc.)
- [ ] **Determine if information blocking regulations apply** to your organization
- [ ] **Assess whether an exception applies** (typically Privacy Exception for anonymized data)
- [ ] **Review applicable laws**: HIPAA Privacy Rule, state privacy laws, institutional IRB requirements
- [ ] **Identify required data elements** and minimum necessary information for the intended purpose

### Configuration Phase

- [ ] **Select de-identification method**: Safe Harbor (default) or Expert Determination
- [ ] **Review default configuration file** based on HIPAA Safe Harbor
- [ ] **Customize configuration** for your specific use case and compliance requirements
- [ ] **Document configuration decisions** and rationale for audit purposes
- [ ] **Validate configuration** against all 18 Safe Harbor identifiers (if using Safe Harbor)

### Implementation Phase

- [ ] **Test anonymization** on sample datasets before production use
- [ ] **Verify removal of identifiers** through automated and manual review
- [ ] **Implement access controls** for both source and anonymized data
- [ ] **Establish audit logging** for all anonymization operations
- [ ] **Train staff** on proper tool usage and compliance requirements

### Validation Phase

- [ ] **Conduct privacy risk assessment** on anonymized output
- [ ] **Review for residual identifiers** not captured by configuration
- [ ] **Assess re-identification risk** in context of intended use and data recipients
- [ ] **Obtain expert determination** if using that method (qualified statistician/expert)
- [ ] **Document compliance** with chosen de-identification method

### Operational Phase

- [ ] **Maintain audit trails** of anonymization operations
- [ ] **Monitor for configuration drift** and updates to tools
- [ ] **Review and update configurations** as data models change (FHIR version updates, etc.)
- [ ] **Periodic re-validation** of anonymization effectiveness
- [ ] **Incident response plan** for any potential privacy breaches

---

## ONC Regulations Reference

### Primary Regulatory Citations

- **45 CFR Part 171** - Information Blocking
  - Subpart A (§ 171.101-103): General provisions and definitions
  - Subpart B (§ 171.201-205): Exceptions that involve not fulfilling requests to access, exchange, or use EHI
  - Subpart C (§ 171.301-303): Exceptions that involve procedures for fulfilling requests

- **45 CFR Parts 160 and 164** - HIPAA Privacy, Security, and Breach Notification Rules
  - § 164.502: Uses and disclosures of PHI - general rules
  - § 164.514(a): De-identification standard
  - § 164.514(b): Safe Harbor and Expert Determination methods

### Key Federal Register Notices

1. **21st Century Cures Act: Interoperability, Information Blocking, and the ONC Health IT Certification Program**
   - Federal Register: Vol. 85, No. 85 (May 1, 2020)
   - RIN: 0955-AA01
   - Final Rule establishing information blocking regulations

2. **HIPAA Privacy Rule - De-identification Guidance**
   - Guidance on de-identification of protected health information
   - Available at: [HHS.gov De-identification Guidance](https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html)

### ONC Resources

- **Information Blocking FAQs**: [https://www.healthit.gov/topic/information-blocking](https://www.healthit.gov/topic/information-blocking)
- **ONC Fact Sheets**: Available at [HealthIT.gov](https://www.healthit.gov)
- **Cures Act Final Rule Resources**: Implementation specifications and guidance documents

---

## Implementation Examples

### Example 1: Safe Harbor Configuration for Research Dataset

```json
{
  "fhirVersion": "R4",
  "processingErrors": "raise",
  "fhirPathRules": [
    {
      "path": "nodesByType('HumanName')",
      "method": "redact"
    },
    {
      "path": "nodesByType('Address').line",
      "method": "redact"
    },
    {
      "path": "nodesByType('Address').city",
      "method": "redact"
    },
    {
      "path": "nodesByType('Address').postalCode",
      "method": "generalize",
      "cases": [
        {
          "regex": "^(\\d{3})\\d{2}$",
          "output": "$100"
        }
      ]
    },
    {
      "path": "nodesByType('ContactPoint')",
      "method": "redact"
    },
    {
      "path": "nodesByType('date')",
      "method": "dateShift",
      "dateShiftKey": "Patient.id",
      "dateShiftScope": "resource"
    },
    {
      "path": "Patient.birthDate",
      "method": "generalize",
      "cases": [
        {
          "regex": "^(\\d{4})-\\d{2}-\\d{2}$",
          "output": "$1"
        }
      ]
    },
    {
      "path": "nodesByType('Identifier').where(system='http://hl7.org/fhir/sid/us-ssn')",
      "method": "redact"
    },
    {
      "path": "nodesByType('Identifier').where(system.contains('mrn'))",
      "method": "cryptoHash",
      "cryptoHashKey": "your-secret-key"
    }
  ],
  "parameters": {
    "dateShiftRange": 50,
    "dateShiftSeed": "anonymization-seed-value",
    "cryptoHashKey": "your-secret-key",
    "enablePartialDatesForRedact": true
  }
}
```

### Example 2: Public Health Reporting with Geographic Generalization

For reporting to state health departments while protecting locality information:

```json
{
  "fhirPathRules": [
    {
      "path": "Patient.address.city",
      "method": "redact"
    },
    {
      "path": "Patient.address.state",
      "method": "keep"
    },
    {
      "path": "Patient.address.postalCode",
      "method": "generalize",
      "cases": [
        {
          "regex": "^(\\d{3})\\d{2}$",
          "output": "$1XX"
        }
      ]
    },
    {
      "path": "Patient.birthDate",
      "method": "generalize",
      "cases": [
        {
          "regex": "^(\\d{4})-\\d{2}-\\d{2}$",
          "output": "$1-01-01"
        }
      ]
    }
  ]
}
```

### Example 3: DICOM De-identification for Research

For medical imaging research, anonymize DICOM metadata while preserving imaging data:

```json
{
  "tagOperations": [
    {
      "tags": ["(0010,0010)"],
      "operation": "redact"
    },
    {
      "tags": ["(0010,0020)"],
      "operation": "cryptoHash"
    },
    {
      "tags": ["(0010,0030)"],
      "operation": "dateShift",
      "dateShiftRange": 100
    },
    {
      "tags": ["(0008,0020)"],
      "operation": "dateShift",
      "dateShiftRange": 100
    }
  ]
}
```

See [DICOM Anonymization Documentation](../DICOM-anonymization.md) for complete DICOM configuration details.

---

## Disclaimers and Responsibilities

### Tool Capabilities vs. Organizational Compliance

**What This Tool Provides:**
- Technical capabilities to anonymize FHIR and DICOM data
- Default configurations based on HIPAA Safe Harbor method
- Flexible rule engine to implement various de-identification strategies
- Audit logging and transparency in anonymization operations

**What Organizations Must Ensure:**
- **Legal compliance determination**: Organizations must determine their specific compliance obligations under the Cures Act, HIPAA, state laws, and institutional policies
- **Configuration appropriateness**: Organizations are responsible for selecting and validating appropriate anonymization configurations for their use cases
- **Expert determination**: If using the Expert Determination method, organizations must engage qualified experts
- **Risk assessment**: Organizations must conduct privacy risk assessments for their specific data sharing scenarios
- **Operational safeguards**: Organizations must implement appropriate access controls, audit procedures, and incident response plans
- **Good faith implementation**: Organizations must use the Privacy Exception in good faith to protect privacy, not to block information inappropriately

### No Legal Advice

This documentation provides **technical guidance only** and does not constitute legal advice. The information blocking regulations and HIPAA requirements are complex and may vary based on:
- Specific organizational circumstances
- State privacy laws and regulations
- Contractual obligations and institutional policies
- Nature of the data and intended uses
- Recipient capabilities and safeguards

**Organizations should consult with qualified legal counsel, privacy officers, and compliance experts** to determine their specific obligations and ensure their use of these tools meets all applicable requirements.

### Bring Your Own Data

As stated in the project README:

> This project provides you the scripts and command line tools for your own use. It **does NOT** and **cannot** access, use, collect, or manage any of your data, including any personal or health-related data. You must bring your own data, and be 100% responsible for using our tools to work with your own data.

**You are 100% responsible for:**
- The data you process with these tools
- Determining appropriate anonymization methods
- Validating anonymization effectiveness
- Compliance with all applicable laws and regulations
- Data security throughout the anonymization process

---

## Additional Resources

### FHIR and DICOM Anonymization

- [FHIR Anonymization Documentation](../FHIR-anonymization.md) - Detailed technical guide for FHIR data
- [DICOM Anonymization Documentation](../DICOM-anonymization.md) - Detailed technical guide for DICOM data
- [Project Vision](../../VISION.md) - Core principles and technical boundaries

### Regulatory and Compliance Resources

- **ONC Information Blocking**: [https://www.healthit.gov/topic/information-blocking](https://www.healthit.gov/topic/information-blocking)
- **HHS HIPAA De-identification Guidance**: [https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html](https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html)
- **Federal Register - Cures Act Final Rule**: [https://www.federalregister.gov/documents/2020/05/01/2020-07419/21st-century-cures-act-interoperability-information-blocking-and-the-onc-health-it-certification](https://www.federalregister.gov/documents/2020/05/01/2020-07419/21st-century-cures-act-interoperability-information-blocking-and-the-onc-health-it-certification)
- **45 CFR Part 171**: [https://www.ecfr.gov/current/title-45/subtitle-A/subchapter-D/part-171](https://www.ecfr.gov/current/title-45/subtitle-A/subchapter-D/part-171)

### Community and Support

- **GitHub Repository**: [https://github.com/microsoft/Tools-for-Health-Data-Anonymization](https://github.com/microsoft/Tools-for-Health-Data-Anonymization)
- **Issues and Discussions**: Report issues or ask questions through GitHub Issues
- **Contributing**: See [Contributing Guidelines](../../CODE_OF_CONDUCT.md) for how to contribute

---

## Document Version and Updates

**Version**: 1.0  
**Last Updated**: February 2026  
**Regulatory Basis**: 45 CFR Part 171 (effective April 5, 2021); 45 CFR Parts 160 and 164 (HIPAA Privacy Rule)

This document will be updated as regulations evolve and new guidance is issued by ONC, HHS, and other regulatory bodies. Check the GitHub repository for the latest version.

---

*This documentation aligns with the project's core principle: "We provide tools; users ensure compliance." Organizations using these tools must conduct their own compliance assessments and obtain appropriate legal and privacy expertise.*