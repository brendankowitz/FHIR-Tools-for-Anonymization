# Vision: Tools for Health Data Anonymization

## Mission

**Protect patient privacy while enabling healthcare innovation.**

We provide robust, compliant tools that enable organizations to safely anonymize FHIR and DICOM healthcare data for secondary use - research, public health analytics, population health management, and data sharing - without compromising individual privacy.

## Core Principles

### 1. Privacy by Design
Every default, every algorithm, every configuration choice prioritizes patient privacy. When in doubt, anonymize more aggressively.

### 2. Correctness over Performance
Anonymization must be mathematically and semantically correct. A fast but incorrect anonymization is worse than no anonymization. We never trade correctness for speed.

### 3. Configuration-Driven Flexibility
Organizations have different compliance requirements (HIPAA Safe Harbor, Expert Determination, GDPR, etc.). Our tools adapt to requirements through configuration, not code changes.

### 4. Fail Secure
On any error, ambiguity, or unexpected condition, the system must fail in a way that protects data. Never emit potentially identifiable information on failure paths.

### 5. Transparency and Auditability
Every anonymization operation should be traceable. Organizations must be able to demonstrate compliance through clear audit trails.

## Technical Boundaries

### What We Anonymize
- **FHIR** - JSON and NDJSON formats
- **DICOM metadata** - All standard and private tags
- **Supported methods**: redact, dateShift, cryptoHash, encrypt, substitute, perturb, generalize

### What We Do Not Do
- Store, transmit, or access user data (users bring their own data)
- Anonymize DICOM pixel data (out of scope - use specialized imaging tools)
- Make compliance determinations (we provide tools; users ensure compliance)

## Quality Standards

- All anonymization algorithms must have comprehensive test coverage
- Security-sensitive code requires peer review
- Configuration changes must be backward compatible
- Public APIs must have XML documentation

## Target Users

- Healthcare IT administrators
- Research data managers
- Health information exchanges
- Public health agencies
- Cloud and on-premises deployments

## Non-Goals

- GUI or visual tooling (we are CLI/SDK focused)
- Real-time streaming anonymization (batch processing focus)
- Data synthesis or generation (we anonymize, not create)
- Legacy format support beyond FHIR STU3

---

*This document guides autonomous agents in maintaining the FHIR Tools for Anonymization project in alignment with its healthcare mission.*
