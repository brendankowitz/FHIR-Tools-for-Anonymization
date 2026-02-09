# Tools for Health Data Anonymization

[![Build Status](https://microsofthealthoss.visualstudio.com/FhirAnonymizer/_apis/build/status/CI%20Build?branchName=master)](https://microsofthealthoss.visualstudio.com/FhirAnonymizer/_build/latest?definitionId=23&branchName=master)

---
**Privacy Notice and Consent**

This project provides you the scripts and command line tools for your own use. It **does NOT** and **cannot** access, use, collect, or manage any of your data, including any personal or health-related data. You must bring your own data, and be 100% responsible for using our tools to work with your own data.

---

**Tools for Health Data Anonymization** is an open-source project that helps anonymize healthcare data, on-premises or in the cloud, for secondary usage such as research, public health, and more. The project first released the anonymization of [FHIR](https://www.hl7.org/fhir/) data to open source on Friday, March 6th, 2020. Currently, it supports both **FHIR data anonymization** and **DICOM data anonymization**.

* For information on FHIR data anonymization, please check out the [FHIR anonymization documentation](docs/FHIR-anonymization.md).
* For information on DICOM data anonymization, please check out the [DICOM anonymization documentation](docs/DICOM-anonymization.md).

## Regulatory Compliance Configurations

The anonymization core engine uses a configuration file specifying different parameters as well as anonymization methods for different data-elements and datatypes. The repo includes pre-configured templates for different regulatory frameworks:

### HIPAA Safe Harbor (United States)
The default configuration is based on the [HIPAA Safe Harbor](https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html#safeharborguidance) method, covering 17 of the 18 identifier types specified in Safe Harbor Method (2)(i). Configuration files: `configuration-sample.json`

### GDPR Article 89 (European Union)
A configuration aligned with EU GDPR requirements for scientific research purposes, implementing:
- **Pseudonymization** for research linkage while protecting privacy
- **Stricter redaction** of direct identifiers (names, addresses, detailed locations)
- **Enhanced protection** for special category data (genetic, biometric, health data)
- **Date shifting** with preserved temporal relationships
- **Data minimization** principles per GDPR Article 5(1)(c)

Configuration files: `configuration-gdpr-article89.json` ([documentation](docs/GDPR-Article89-configuration.md), [compliance mapping](docs/GDPR-compliance-mapping.md), [examples](FHIR/samples/gdpr-examples/README.md))

**Important**: These configurations provide technical safeguards but do not constitute legal compliance. Organizations must:
- Conduct Data Protection Impact Assessments (DPIAs) for high-risk processing
- Establish appropriate legal basis (consent, public interest, legitimate interest)
- Implement organizational safeguards (access controls, audit logging, data processing agreements)
- Consult with Data Protection Officers and legal counsel
- Comply with applicable national implementations and sector-specific regulations

You can modify these configurations or create your own as needed.

## Project Support

This open source project is fully backed by the Microsoft Healthcare team, but we know that this project will only get better with your feedback and contributions. We are leading the development of this code base, and test builds and deployments daily.

FHIR® is the registered trademark of HL7 and is used with the permission of HL7. Use of the FHIR trademark does not constitute endorsement of this product by HL7.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

FHIR® is the registered trademark of HL7 and is used with the permission of HL7.

## Quick Start Commands

The following commands help you get started building and testing the anonymization tools quickly.

- Build all: `dotnet build DICOM\Dicom.Anonymizer.sln && dotnet build FHIR\Fhir.Anonymizer.sln`
- Build DICOM only: `dotnet build DICOM\Dicom.Anonymizer.sln`
- Build FHIR only: `dotnet build FHIR\Fhir.Anonymizer.sln`
- Build specific framework: `dotnet build DICOM\Dicom.Anonymizer.sln -f net8.0`
- Build with .NET 10: `dotnet build DICOM\Dicom.Anonymizer.sln -f net10.0`
- Run tests: `dotnet test DICOM\Dicom.Anonymizer.sln && dotnet test FHIR\Fhir.Anonymizer.sln`
- Create packages: `dotnet pack DICOM\Dicom.Anonymizer.sln -o packages && dotnet pack FHIR\Fhir.Anonymizer.sln -o packages`
