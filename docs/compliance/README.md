# Compliance Documentation

This directory contains compliance guidance for using the Tools for Health Data Anonymization in regulated healthcare environments.

## Overview

Healthcare organizations face complex and overlapping compliance obligations when handling, storing, and sharing health data. These tools help organizations meet their privacy protection responsibilities while enabling valuable secondary uses of healthcare data.

**Important**: This documentation provides **technical guidance only** and does not constitute legal advice. Organizations must consult with qualified legal counsel, privacy officers, and compliance experts to determine their specific obligations.

---

## Available Compliance Guides

### [21st Century Cures Act and Information Blocking](cures-act.md)

Comprehensive guide on using anonymization to comply with the Cures Act's data sharing mandates while maintaining privacy protection under the Privacy Exception to information blocking regulations.

**Key Topics:**
- Information blocking regulations and the eight exceptions
- Privacy Exception and how anonymization enables compliant data sharing
- HIPAA Safe Harbor method implementation with this tool
- Use cases: research, public health, quality improvement, patient access, AI/ML training
- Compliance checklist for organizations
- ONC regulatory citations (45 CFR Part 171)
- Implementation examples and configurations

**Audience:** Healthcare providers, HIEs, HINs, health IT developers subject to information blocking regulations

---

## Compliance Framework

### Core Regulatory Areas

1. **HIPAA Privacy Rule** (45 CFR Parts 160 and 164)
   - De-identification standards (Safe Harbor and Expert Determination)
   - Permitted uses and disclosures
   - Minimum necessary standard

2. **21st Century Cures Act** (45 CFR Part 171)
   - Information blocking prohibition
   - Eight exceptions to information blocking
   - Requirements for healthcare providers, HIEs, HINs, and health IT developers

3. **State Privacy Laws**
   - Varying requirements across jurisdictions
   - May be more stringent than federal requirements
   - Organizations must comply with applicable state laws

4. **Research and Institutional Requirements**
   - IRB oversight for research use
   - Common Rule protections for human subjects research
   - Institutional policies and data use agreements

---

## How Anonymization Supports Compliance

### De-identification Removes HIPAA Restrictions

Under HIPAA, properly de-identified health information is **not** considered Protected Health Information (PHI). Once de-identified, data can be:

- Shared without patient authorization
- Used for research without IRB approval for PHI use
- Disclosed to third parties without Business Associate Agreements
- Stored with fewer security controls than required for PHI
- Combined across organizations for analytics

### Privacy Exception to Information Blocking

The Cures Act's **Privacy Exception** (45 CFR § 171.202) permits organizations to limit information sharing to protect patient privacy, provided the practice is:
- Consistent with applicable law (including HIPAA)
- Non-discriminatory
- Implemented in good faith

**Anonymization is a key strategy** for invoking the Privacy Exception while still enabling data sharing for valuable purposes.

---

## Tool Capabilities for Compliance

### HIPAA Safe Harbor Implementation

The default configuration implements the HIPAA Safe Harbor method by removing or generalizing the 18 specified identifiers:

```
✓ Names                    → redact, substitute
✓ Geographic data          → generalize (keep ZIP-3 only)
✓ Dates                    → dateShift, generalize (to year)
✓ Contact information      → redact
✓ Identifiers (SSN, MRN)   → redact, cryptoHash
✓ URLs, IPs                → redact
✓ Biometric identifiers    → redact
```

### Flexible Configuration for Various Requirements

Organizations can customize anonymization rules through JSON configuration files to meet:
- Different de-identification standards (Safe Harbor, Expert Determination)
- Varying data utility needs for different use cases
- State-specific privacy requirements
- Institutional policies

### Audit and Transparency

The tool provides:
- Configurable logging of anonymization operations
- Transparent rule evaluation
- Reproducible anonymization (with consistent seeds/keys)
- Version-controlled configurations

---

## Organizational Responsibilities

### What This Tool Provides

✅ Technical capabilities to anonymize FHIR and DICOM data  
✅ Default configurations based on HIPAA Safe Harbor  
✅ Flexible rule engine for custom de-identification strategies  
✅ Transparent and auditable anonymization process  

### What Organizations Must Ensure

🔲 **Legal compliance determination** - Assess your specific obligations  
🔲 **Configuration validation** - Ensure rules meet your requirements  
🔲 **Privacy risk assessment** - Evaluate re-identification risk in your context  
🔲 **Expert engagement** - Consult legal, privacy, and compliance experts  
🔲 **Operational safeguards** - Implement access controls and monitoring  
🔲 **Documentation** - Maintain audit trails and compliance records  

---

## Compliance Checklist

Before using these tools in production, organizations should:

- [ ] Identify applicable regulations (HIPAA, Cures Act, state laws)
- [ ] Determine the purpose and recipients of data sharing
- [ ] Select appropriate de-identification method (Safe Harbor or Expert Determination)
- [ ] Review and customize configuration files for your use case
- [ ] Test anonymization on sample data
- [ ] Validate removal of all applicable identifiers
- [ ] Conduct privacy risk assessment
- [ ] Document compliance rationale and decisions
- [ ] Implement access controls for source and anonymized data
- [ ] Establish audit logging and monitoring
- [ ] Train staff on proper usage and compliance requirements
- [ ] Obtain legal and privacy officer sign-off
- [ ] Establish periodic review and re-validation procedures

---

## Disclaimer

### No Legal Advice

This documentation provides **technical guidance only** and does not constitute legal advice. Compliance requirements are complex and vary based on:

- Specific organizational circumstances
- Applicable federal, state, and local laws
- Nature of the data and intended uses
- Recipient capabilities and safeguards
- Contractual and institutional obligations

**Organizations must consult with qualified legal counsel, privacy officers, and compliance experts** to determine their specific obligations and ensure appropriate use of these tools.

### Bring Your Own Data

As stated in the project README:

> This project provides you the scripts and command line tools for your own use. It **does NOT** and **cannot** access, use, collect, or manage any of your data, including any personal or health-related data. You must bring your own data, and be 100% responsible for using our tools to work with your own data.

**You are 100% responsible for:**
- The data you process
- Determining appropriate anonymization methods
- Validating anonymization effectiveness
- Ensuring compliance with all applicable laws
- Maintaining data security throughout processing

---

## Additional Resources

### Technical Documentation

- [FHIR Anonymization Documentation](../FHIR-anonymization.md)
- [DICOM Anonymization Documentation](../DICOM-anonymization.md)
- [Project Vision and Principles](../../VISION.md)

### Regulatory Resources

- **ONC Information Blocking**: [https://www.healthit.gov/topic/information-blocking](https://www.healthit.gov/topic/information-blocking)
- **HHS HIPAA De-identification**: [https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html](https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html)
- **45 CFR Part 171**: [https://www.ecfr.gov/current/title-45/subtitle-A/subchapter-D/part-171](https://www.ecfr.gov/current/title-45/subtitle-A/subchapter-D/part-171)
- **Cures Act Final Rule**: [Federal Register Vol. 85, No. 85 (May 1, 2020)](https://www.federalregister.gov/documents/2020/05/01/2020-07419/21st-century-cures-act-interoperability-information-blocking-and-the-onc-health-it-certification)

### Community

- **GitHub Repository**: [https://github.com/microsoft/Tools-for-Health-Data-Anonymization](https://github.com/microsoft/Tools-for-Health-Data-Anonymization)
- **Report Issues**: Use GitHub Issues for technical questions or bug reports
- **Contributing**: See repository contributing guidelines

---

## Document Updates

These compliance guides are maintained as regulatory guidance evolves. Check the GitHub repository for the latest versions and updates.

**Last Updated**: February 2026

---

*This documentation aligns with the project's core principle: "We provide tools; users ensure compliance."*