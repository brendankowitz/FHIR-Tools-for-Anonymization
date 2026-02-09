# GDPR Article 89 Configuration: Compliance Mapping

This document maps each anonymization rule in the GDPR Article 89 configuration to specific GDPR requirements, explaining the legal and technical rationale.

## Configuration Structure Overview

```
Configuration Components:
├── fhirVersion (R4 or STU3)
├── processingErrors (fail/raise)
├── fhirPathRules (resource-type specific rules)
├── parameters
│   ├── dateShiftRange
│   ├── dateShiftKeyPrefix
│   ├── encryptKey
│   ├── enablePartialAgesForRedact
│   ├── enablePartialDatesForRedact
│   └── enablePartialZipCodesForRedact
└── pathRules (global, cross-resource rules)
```

## Legal Framework

### GDPR Article 89(1) - Full Text

> "Processing for archiving purposes in the public interest, scientific or historical research purposes or statistical purposes, shall be subject to appropriate safeguards, in accordance with this Regulation, for the rights and freedoms of the data subject. Those safeguards shall ensure that **technical and organisational measures** are in place in particular in order to ensure respect for the principle of **data minimisation**. Those measures may include **pseudonymisation** provided that those purposes can be fulfilled in that manner."

### Key Principles Applied

| GDPR Principle | Article | Implementation in Configuration |
|----------------|---------|--------------------------------|
| **Data Minimization** | 5(1)(c) | Only retain data elements necessary for research; redact narratives, names, contacts |
| **Purpose Limitation** | 5(1)(b) | Configuration designed for specific research purposes (must be documented by user) |
| **Integrity & Confidentiality** | 5(1)(f) | Pseudonymization (cryptoHash) protects against unauthorized identification |
| **Pseudonymization** | 4(5), 89(1) | Applied to identifiers instead of complete removal |
| **Special Categories Protection** | 9(1), 9(2)(j) | Stricter handling of health, genetic, biometric data |

## Rule-by-Rule Compliance Mapping

### 1. Pseudonymization Rules (cryptoHash)

#### Rule: Pseudonymize all ID elements

```json
{
  "path": "nodesByType.id",
  "method": "cryptoHash",
  "hashFunction": "SHA256"
}
```

**GDPR Mapping:**
- **Article 4(5)**: Implements pseudonymization definition
- **Article 89(1)**: Uses pseudonymization as appropriate safeguard
- **Recital 28**: Reduces data subject identification risks
- **Article 32(1)(a)**: Cryptographic hash is a technical security measure

**Rationale:**
- **Research Utility**: Maintains ability to link records across resources (Patient → Observation → Condition)
- **Privacy Protection**: SHA256 hash prevents direct identification
- **Consistency**: Same identifier always produces same pseudonym (deterministic hashing)

**Difference from HIPAA:** HIPAA Safe Harbor requires complete removal of IDs. GDPR Article 89 allows pseudonymization to maintain research utility.

#### Rule: Pseudonymize Resource References

```json
{
  "path": "nodesByType.Reference.reference",
  "method": "cryptoHash",
  "hashFunction": "SHA256"
}
```

**GDPR Mapping:**
- **Article 5(1)(c)**: Data minimization - preserves only reference structure, not identifying content
- **Article 89(1)**: Enables research on linked records while pseudonymizing identifiers

**Rationale:** References like `Patient/12345` must be pseudonymized to prevent identification while maintaining FHIR resource relationships.

---

### 2. Date Shifting Rules

#### Rule: Date Shift with ±90 Days Range

```json
{
  "path": "nodesByType.dateTime",
  "method": "dateShift",
  "dateShiftRange": 90,
  "dateShiftScope": "resource"
}
```

**GDPR Mapping:**
- **Article 5(1)(c)**: Data minimization - obscures exact dates
- **Recital 26**: Dates can be indirect identifiers when combined with other data
- **WP29 Opinion 05/2014**: Recommends generalization or perturbation of dates

**Rationale:**
- **Privacy**: Exact dates (especially rare events like transplants) can enable identification
- **Utility**: Maintains temporal relationships and intervals between events
- **Consistency**: Same patient receives same date shift across all resources

**Parameters:**

```json
"dateShiftRange": 90,
"enablePartialDatesForRedact": false,
"enablePartialAgesForRedact": false
```

**GDPR Mapping:**
- **Article 5(1)(c)**: Data minimization requires not retaining year-only dates
- **Stricter than HIPAA**: HIPAA allows year retention; GDPR does not have this safe harbor provision

**Rationale:** Partial dates (year only, YYYY-MM only) can be identifying, especially for rare conditions or small populations. GDPR requires more conservative approach.

---

### 3. Redaction Rules

#### Rule: Redact All Narratives

```json
{
  "path": "nodesByType.Narrative",
  "method": "redact"
},
{
  "path": "nodesByType.Narrative.div",
  "method": "redact"
}
```

**GDPR Mapping:**
- **Article 5(1)(c)**: Data minimization - free text not essential for most research
- **Recital 26**: Free text may contain unexpected identifiers
- **Article 9**: Special category data (health information) requires strict protection

**Rationale:** Narratives are human-readable summaries that:
- Often contain names, dates, locations
- May include sensitive details not captured in codes
- Are high-risk for inadvertent disclosure
- Rarely essential for quantitative research

#### Rule: Redact Names

```json
{
  "path": "nodesByType.HumanName",
  "method": "redact"
},
{
  "path": "Patient.name",
  "method": "redact"
},
{
  "path": "Practitioner.name",
  "method": "redact"
}
```

**GDPR Mapping:**
- **Article 4(1)**: Names are direct identifiers of natural persons
- **Article 5(1)(c)**: Not necessary for research purposes (patient identity managed via pseudonym)
- **Recital 26**: Named individuals = personal data

**Rationale:** Names are the most obvious direct identifier and must be removed. Research can proceed using pseudonymized IDs.

#### Rule: Redact Contact Information

```json
{
  "path": "nodesByType.ContactPoint",
  "method": "redact"
},
{
  "path": "Patient.telecom",
  "method": "redact"
}
```

**GDPR Mapping:**
- **Article 4(1)**: Phone numbers, email addresses are identifiers
- **EDPB Guidelines 07/2020**: Contact details are personal data
- **Article 5(1)(c)**: Not necessary for health research purposes

**Rationale:** Telecom data serves no research purpose and creates re-identification risk.

#### Rule: Redact Addresses

```json
{
  "path": "nodesByType.Address",
  "method": "redact"
},
{
  "path": "Patient.address",
  "method": "redact"
}
```

**GDPR Mapping:**
- **Article 4(1)**: Addresses identify or make identifiable a natural person
- **WP29 Opinion 05/2014**: Geographic data below country level can be identifying
- **Article 5(1)(c)**: Precise addresses rarely necessary for health research

**Rationale:** 
- Full addresses are direct identifiers
- Even partial addresses (city, postal code) can enable identification in small populations
- More conservative than HIPAA (which allows 3-digit ZIP codes)

**Note:** If regional analysis is required, consider keeping only country or large regions with explicit justification in DPIA.

#### Rule: Redact Photos and Attachments

```json
{
  "path": "Patient.photo",
  "method": "redact"
},
{
  "path": "nodesByType.Attachment",
  "method": "redact"
}
```

**GDPR Mapping:**
- **Article 4(14)**: Photos are biometric data (special category)
- **Article 9(1)**: Prohibition on processing biometric data (with Article 9(2)(j) research exception if safeguards applied)
- **Article 9(1)**: Health data in documents requires special protection
- **Recital 51**: Photographs are personal data

**Rationale:**
- Photos are direct identifiers and biometric data
- Attachments may contain identifying information (PDFs, images, documents)
- Rarely necessary for research purposes

---

### 4. Special Category Data Handling

#### Rule: Redact Genetic Data

```json
{
  "path": "Observation.where(code.coding.system='http://loinc.org' and code.coding.code='51969-4')",
  "method": "redact"
}
```

**GDPR Mapping:**
- **Article 4(13)**: Genetic data definition (data relating to inherited or acquired genetic characteristics)
- **Article 9(1)**: Special category requiring heightened protection
- **Recital 34**: Genetic data warrants specific protection

**Rationale:**
- Genetic sequences are highly identifying
- Can reveal information about relatives (family members' privacy)
- Article 89 allows processing with safeguards, but full sequences rarely needed

**Alternative:** If genetic research is the purpose, consider:
- Aggregated allele frequencies instead of individual sequences
- Specific variants only (not full genome)
- Additional access controls and encryption

---

### 5. Data Retention Rules (Keep)

#### Rule: Keep Observation Values and Codes

```json
{
  "path": "Observation.value",
  "method": "keep"
},
{
  "path": "Observation.code",
  "method": "keep"
}
```

**GDPR Mapping:**
- **Article 89(1)**: Research purposes justify retention when safeguards (pseudonymization, redaction of identifiers) are in place
- **Article 5(1)(b)**: Purpose limitation - clinical data necessary for health research purposes
- **Article 5(1)(c)**: Data minimization - retain only what's necessary

**Rationale:**
- Observation values (lab results, vital signs) are core research data
- Combined with pseudonymization and identifier removal, retention is justified
- Codes (LOINC, SNOMED) provide structured, analyzable data without free-text risks

#### Rule: Keep Condition/Diagnosis Codes

```json
{
  "path": "Condition.code",
  "method": "keep"
}
```

**GDPR Mapping:**
- **Article 9(2)(j)**: Research processing of health data allowed with safeguards
- **Article 89(1)**: Clinical codes necessary for health research purposes
- **Recital 53**: Health data includes diagnoses and medical conditions

**Rationale:**
- ICD-10, SNOMED CT codes are essential for disease research
- Structured codes have lower identification risk than narratives
- Pseudonymization of associated patient ID provides safeguard

**Risk Management:**
- Very rare diagnoses (e.g., ultra-rare genetic conditions) may still pose identification risk in small populations
- Consider generalization or removal if dataset is small and condition is highly identifying

---

### 6. Text Redaction in Codes

#### Rule: Redact Display Text in CodeableConcept

```json
{
  "path": "nodesByType.CodeableConcept.text",
  "method": "redact"
},
{
  "path": "nodesByType.Coding.display",
  "method": "redact"
}
```

**GDPR Mapping:**
- **Article 5(1)(c)**: Data minimization - display text is redundant if code is retained
- **Recital 26**: Free text may contain additional identifying information

**Rationale:**
- Display text like "John Smith's diabetes" could contain identifying info
- Codes alone (e.g., ICD-10 E11.9) provide necessary clinical information
- Removes risk of clinician adding identifying details in free-text fields

---

### 7. Configuration Parameters

#### Parameter: Processing Errors = "raise"

```json
"processingErrors": "raise"
```

**GDPR Mapping:**
- **Article 5(1)(f)**: Integrity and confidentiality (security principle)
- **Article 32**: Security of processing
- **Recital 83**: Ability to detect and respond to failures

**Rationale:**
- **Fail-secure**: If anonymization fails, processing stops
- Prevents accidental output of non-anonymized data
- Aligns with security by design principles

#### Parameter: Disable Partial Retention

```json
"enablePartialAgesForRedact": false,
"enablePartialDatesForRedact": false,
"enablePartialZipCodesForRedact": false
```

**GDPR Mapping:**
- **Article 5(1)(c)**: Data minimization - no retention of partial identifiers
- **WP29 Opinion 05/2014**: Warns against quasi-identifiers (age, date, location combinations)
- **Recital 26**: Combination of attributes can enable identification

**Rationale:**
- GDPR has no equivalent to HIPAA Safe Harbor's specific allowances (ages 90+, 3-digit ZIP, year only)
- European DPAs generally require more conservative approach
- Small populations and detailed codes increase re-identification risk from quasi-identifiers

**Difference from HIPAA:** HIPAA explicitly allows these partial retentions; GDPR requires case-by-case risk assessment.

---

## Special Considerations

### Article 9 - Special Categories of Personal Data

GDPR Article 9(1) prohibits processing of:
- Health data
- Genetic data
- Biometric data
- Racial/ethnic origin data
- Religious beliefs
- Political opinions
- Trade union membership
- Sexual orientation

**Article 9(2)(j) Exception:**

> Processing is necessary for archiving purposes in the public interest, scientific or historical research purposes [...] in accordance with Article 89(1) based on Union or Member State law [...]

**Configuration Implementation:**

| Special Category | Configuration Approach |
|------------------|------------------------|
| **Health data** | Structured codes kept; narratives and free-text redacted; identifiers pseudonymized |
| **Genetic data** | Full sequences redacted; specific variants may be kept with justification |
| **Biometric data** | Photos, fingerprints, voice recordings redacted |
| **Racial/ethnic origin** | Extension data and specific observations redacted unless essential for research |

### Recital 26 - Anonymized Data Outside GDPR Scope

> "The principles of data protection should [...] not apply to anonymous information, namely information which does not relate to an identified or identifiable natural person or to personal data rendered anonymous in such a manner that the data subject is not or no longer identifiable."

**Important:** This configuration produces **pseudonymized** data, not fully anonymized data:
- Data remains in GDPR scope
- Data subject rights apply (with Article 89 derogations)
- Re-identification is theoretically possible
- Security and access controls still required

**To achieve true anonymization:**
- Conduct expert determination assessment
- Consider k-anonymity, l-diversity techniques
- Aggregate data where possible
- Regularly reassess against external datasets

### Recital 156 - Research Processing

> "Scientific research purposes should be interpreted in a broad manner [...] technological development and demonstration, fundamental research, applied research and privately funded research [...] comply with recognised ethical standards [...] and be subject to appropriate safeguards for the rights and freedoms of the data subject."

**Configuration Alignment:**
- Broad applicability (clinical trials, epidemiology, health services research)
- Must be used within ethical framework (ethics committee approval)
- Safeguards implemented (pseudonymization, minimization, access controls)

---

## Compliance Checklist

Use this checklist to verify GDPR Article 89 compliance:

### Legal Basis (Article 6)
- [ ] Legal basis for processing documented (consent, public interest, legitimate interest)
- [ ] If based on Member State law, specific law identified
- [ ] If consent-based, consent meets GDPR standards (freely given, specific, informed, unambiguous)

### Special Category Legal Basis (Article 9)
- [ ] Article 9(2) exception identified (typically 9(2)(j) for research)
- [ ] Union or Member State law allowing research processing identified
- [ ] Alternative basis if Article 9(2)(j) not applicable (e.g., explicit consent under 9(2)(a))

### Data Minimization (Article 5(1)(c))
- [ ] Only data elements necessary for research purposes are retained
- [ ] Unnecessary identifiers removed or pseudonymized
- [ ] Free-text fields assessed and redacted where not essential
- [ ] Configuration customized to specific research purpose

### Purpose Limitation (Article 5(1)(b))
- [ ] Research purposes clearly documented
- [ ] Configuration aligned with stated purposes
- [ ] Data will not be used for incompatible purposes

### Pseudonymization (Article 89(1))
- [ ] Pseudonymization applied to identifiers
- [ ] Hash function appropriate (SHA256 or stronger)
- [ ] Re-identification keys (if any) stored separately and securely
- [ ] Access to re-identification restricted

### Security (Article 32)
- [ ] Anonymization is one of multiple security measures
- [ ] Access controls implemented for anonymized data
- [ ] Encryption in transit and at rest
- [ ] Audit logging enabled
- [ ] Regular security assessments conducted

### Data Protection Impact Assessment (Article 35)
- [ ] DPIA conducted (required for large-scale special category processing)
- [ ] Risks to data subjects identified and mitigated
- [ ] Anonymization approach assessed for adequacy
- [ ] DPO consulted (if appointed)
- [ ] Supervisory authority consulted if high risk remains

### Transparency and Accountability (Articles 5(2), 13-14)
- [ ] Data subjects informed of research processing (unless exemption applies)
- [ ] Privacy notice includes information about anonymization
- [ ] Records of processing activities maintained (Article 30)
- [ ] Configuration and procedures documented

### Data Subject Rights (Article 89(2))
- [ ] Understand Article 89(2) allows derogations from certain rights (access, rectification, restriction, objection)
- [ ] Member State law derogations identified (if applicable)
- [ ] Rights that still apply are respected (e.g., right to be informed)

### Ethics and Governance
- [ ] Ethics committee approval obtained (if required)
- [ ] Scientific protocol approved
- [ ] Conflicts of interest disclosed
- [ ] Publication plan adheres to privacy principles

---

## Differences from HIPAA Safe Harbor

### Summary Table

| Element | HIPAA Safe Harbor | GDPR Article 89 (This Config) |
|---------|-------------------|-------------------------------|
| **Legal Framework** | De-identification (data exits HIPAA) | Pseudonymization (data remains in GDPR) |
| **Patient ID** | Remove | Pseudonymize (cryptoHash) |
| **Dates** | Year allowed; dates shifted ±365 days | All dates shifted ±90 days; year NOT retained |
| **Ages** | Ages 90+ aggregated; others allowed | All ages date-shifted; no 90+ aggregation |
| **ZIP Code** | 3-digit allowed if population >20k | Entire address redacted |
| **Medical Record Number** | Remove | Pseudonymize |
| **Device Serial** | Remove | Pseudonymize (maintains device tracking) |
| **Names** | Remove | Redact |
| **Biometric Data** | Photos, fingerprints remove | Redact (Article 9 special category) |
| **Email/Phone/Fax** | Remove | Redact |
| **Clinical Codes** | Allowed | Allowed (Article 89 research exception) |
| **Narratives** | Not explicitly addressed | Redact (data minimization) |
| **Purpose Limitation** | Not part of Safe Harbor | Must document and limit to research purposes |
| **Reversibility** | Irreversible de-identification | Pseudonymization (potentially reversible) |

### Key Philosophical Difference

**HIPAA Safe Harbor:**
- Remove 18 specific identifiers → data exits HIPAA regulation
- Focus: De-identification (one-way, irreversible)
- Result: Data is no longer PHI

**GDPR Article 89:**
- Apply safeguards → data remains under GDPR but processing allowed for research
- Focus: Pseudonymization + data minimization (may be reversible in secure environment)
- Result: Data remains personal data with special processing rules

---

## Additional Resources

### European Data Protection Board (EDPB) Guidance
- [Guidelines 4/2019 on Article 25 Data Protection by Design and by Default](https://edpb.europa.eu/our-work-tools/our-documents/guidelines/guidelines-42019-article-25-data-protection-design-and_en)
- [Guidelines 07/2020 on the concepts of controller and processor](https://edpb.europa.eu/our-work-tools/our-documents/guidelines/guidelines-072020-concepts-controller-and-processor-gdpr_en)

### Article 29 Working Party Opinions (Pre-GDPR but still relevant)
- [Opinion 05/2014 on Anonymisation Techniques](https://ec.europa.eu/justice/article-29/documentation/opinion-recommendation/files/2014/wp216_en.pdf)
- [Opinion 03/2013 on Purpose Limitation](https://ec.europa.eu/justice/article-29/documentation/opinion-recommendation/files/2013/wp203_en.pdf)

### National DPA Guidance (Examples)
- **France (CNIL)**: [Guide to Pseudonymisation](https://www.cnil.fr/en/sheet-ndeg5-pseudonymisation)
- **UK (ICO)**: [Anonymisation Code of Practice](https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/anonymisation/)
- **Germany**: Various Landesdatenschutz guidance on health research

### Scientific Literature
- Emam, K. E., et al. (2011). "A systematic review of re-identification attacks on health data." *PLOS ONE*.
- Majeed, A., & Lee, J. (2020). "Anonymization Techniques for Privacy Preserving Data Publishing: A Comprehensive Survey." *IEEE Access*.

---

## Disclaimer

**This document is for informational and educational purposes only. It does not constitute legal advice.**

GDPR compliance requires holistic assessment of:
- Legal basis for processing
- Technical AND organizational measures
- Governance and accountability
- Data subject rights management
- National law implementations
- Sector-specific regulations (e.g., clinical trials regulation)

**Always consult:**
- Legal counsel with GDPR expertise
- Your Data Protection Officer (DPO)
- Ethics committee (for research)
- Relevant national Data Protection Authority

**Organizations using this configuration are solely responsible for ensuring compliance with applicable laws and regulations.**

---

*Last updated: February 2026*  
*Configuration version: 1.0*  
*Mapping maintained by: Microsoft Health & Life Sciences Team*
