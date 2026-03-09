# FHIR Data Anonymization

FHIR data anonymization is available in the following ways:

1. A command line tool. Can be used on-premises or in the cloud to anonymize data.
2. An Azure Data Factory (ADF) pipeline. Comes with a [script](#anonymize-fhir-data-using-azure-data-factory) to create a pipeline that reads data from Azure blob store and writes anonymized data back to a specified blob store.
3. [De-identified $export](#how-to-perform-de-identified-export-operation-on-the-fhir-server) operation in the [FHIR server for Azure](https://github.com/microsoft/fhir-server).

### Features
* Support anonymization of FHIR R4 and STU3 data in JSON as well as NDJSON format
* Configuration of the data elements that need to be anonymized 
* Configuration of the [anonymization methods](#fhir-path-rules) for each data element
* Ability to create a anonymization pipeline in Azure Data Factory
* Ability to run the tool on premise to anonymize a dataset locally
* **Pre-configured templates for HIPAA Safe Harbor and GDPR Article 89 compliance**

### Building the solution
Use the .Net Core SDK to build FHIR Tools for Anonymization. If you don't have .Net Core installed, instructions and download links are available [here](https://dotnet.microsoft.com/download/dotnet/6.0).

### Get sample FHIR files
This repo contains a few [sample](../FHIR/samples/) FHIR files that you can download. These files were generated using  [Synthea&trade; Patient Generator](https://github.com/synthetichealth/synthea). 

You can also export FHIR resource from your FHIR server using [Bulk Export](https://docs.microsoft.com/en-us/azure/healthcare-apis/configure-export-data).

### Table of Contents

- [Anonymize FHIR data: using the command line tool](#anonymize-fhir-data-using-the-command-line-tool)
- [Anonymize FHIR data: using Azure Data Factory](#anonymize-fhir-data-using-azure-data-factory)
- [Sample configuration file](#sample-configuration-file)
- [Sample rules using FHIR Path](#sample-rules-using-fhir-path)
- [Data anonymization algorithms](#data-anonymization-algorithms)

## Anonymize FHIR data: using the command line tool
Once you have built the command line tool, you will find two executable files for R4 and STU3 respectively: 

1. Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool.exe in the $SOURCE\FHIR\src\Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool\bin\Debug|Release\net8.0 folder. 

2. Microsoft.Health.Fhir.Anonymizer.Stu3.CommandLineTool.exe in the $SOURCE\FHIR\src\Microsoft.Health.Fhir.Anonymizer.Stu3.CommandLineTool\bin\Debug|Release\net8.0 folder.

 You can use these executables to anonymize FHIR resource files in a folder.   
```
> .\Microsoft.Health.Fhir.Anonymizer.<version>.CommandLineTool.exe -i myInputFolder -o myOutputFolder
```

The command-line tool can be used to anonymize a folder containing FHIR resource files. Here are the parameters that the tool accepts:

| Option | Name | Optionality | Default | Description |
| ----- | ----- | ----- |----- |----- |
| -i | inputFolder | Required | | Folder to locate input resource files. |
| -o | outputFolder | Required | |  Folder to save anonymized resource files. |
| -c | configFile | Optional |configuration-sample.json | Anonymizer configuration file path. It reads the default file from the current directory. |
| -b | bulkData | Optional| false | Resource file is in bulk data format (.ndjson). |
| -r | recursive | Optional | false | Process resource files in input folder recursively. |
| -v | verbose | Optional | false | Provide additional details during processing. |
| -s | skip | Optional | false | Skip files that are already present in the destination folder. |
| --validateInput | validateInput | Optional | false | Validate input resources against structure, cardinality and most value domains in FHIR specification. Detailed report can be found in verbose log. |
| --validateOutput | validateOutput | Optional | false | Validate anonymized resources against structure, cardinality and most value domains in FHIR specification. Detailed report can be found in verbose log. |

Example usage to anonymize FHIR resource files in a folder: 
```
> .\Microsoft.Health.Fhir.Anonymizer.<version>.CommandLineTool.exe -i myInputFolder -o myOutputFolder
```

### Using Pre-configured Templates

The tool includes regulatory-specific configuration templates:

#### HIPAA Safe Harbor (Default)
```
> .\Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool.exe -i myInputFolder -o myOutputFolder -c configuration-sample.json
```
Based on HIPAA Safe Harbor Method (2)(i), covering 17 identifier types (A-Q). See [configuration documentation](#sample-configuration-file) for details.

#### GDPR Article 89 (Scientific Research)
```
> .\Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool.exe -i myInputFolder -o myOutputFolder -c configuration-gdpr-article89.json
```
Aligned with EU GDPR Article 89 requirements for scientific research purposes. Includes:
- Pseudonymization of identifiers for research linkage
- Strict redaction of direct identifiers
- Enhanced protection for special category data (genetic, biometric)
- Date shifting with preserved temporal relationships
- Data minimization principles

For detailed guidance, see:
- [GDPR Article 89 Configuration Guide](GDPR-Article89-configuration.md)
- [GDPR Compliance Mapping](GDPR-compliance-mapping.md)
- [GDPR Examples](../FHIR/samples/gdpr-examples/README.md)

**Important**: Configuration templates provide technical safeguards but do not ensure legal compliance. Consult with your Data Protection Officer and legal counsel to verify suitability for your use case and jurisdiction.

## Anonymize FHIR data: using Azure Data Factory

You can use the Azure PowerShell to create a Data Factory and a pipeline to anonymize FHIR data. The pipeline reads from an Azure blob container, anonymizes it as per the configuration file, and writes the output to another blob container. If you're new to Azure Data Factory, see [Introduction to Azure Data Factory](https://docs.microsoft.com/en-us/azure/data-factory/introduction).

* Use a PowerShell script to create a data factory pipeline.
* Trigger on-demand pipeline run.
* Monitor the pipeline and activity runs.

### Prerequisites

* **Azure subscription**: If you don't have an Azure subscription, create a [free account](https://azure.microsoft.com/free/) before you begin.
* **Azure storage account**: Azure Blob storage is used as the _source_ & _destination_ data store. If you don't have an Azure storage account, see the instructions in [Create a storage account](https://docs.microsoft.com/en-us/azure/storage/common/storage-account-create?tabs=azure-portal). 
* **Azure PowerShell**: Azure PowerShell is used for deploying azure resources. If you don't have Azure PowerShell installed, see the instructions in [Install the Azure PowerShell module](https://docs.microsoft.com/en-us/powershell/azure/install-az-ps?view=azps-3.4.0)
* **.Net Core**: Use .Net Core sdk to build FHIR Tools for Anonymization. If you don't have .Net Core installed, instructions and download links are available [here](https://dotnet.microsoft.com/download/dotnet-core/6.0).

#### Prepare azure storage resource container

Create a source and a destination container on your blob store. Upload your FHIR files to the source blob container. The pipeline will read the files from the source container and upload the anonymized files to the destination container.

## Key Length Requirements for Cryptographic Operations

When configuring cryptographic operations (`cryptoHash`, `encrypt`, `dateShift`), keys must meet minimum security requirements enforced at validation time.

### Minimum Key Length

All cryptographic keys (`cryptoHashKey`, `encryptKey`, `dateShiftKey`) must be at least **32 characters** in length. This requirement is based on NIST SP 800-107 Rev. 1 guidance that HMAC keys should be at least as long as the hash output length (SHA-256 = 32 bytes).

Keys that are too short will cause a `SecurityException` at configuration validation time, preventing accidental use of weak keys in production.

### Generating Secure Keys

Use one of the following commands to generate a cryptographically secure random key:

**Linux/macOS:**
```bash
openssl rand -base64 32
```

**Windows (PowerShell):**
```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

**.NET:**
```csharp
var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
```

### Key Validation Rules

The tool validates all cryptographic keys and will throw a `SecurityException` for:

| Condition | Behaviour |
|---|---|
| Null or empty key | Allowed — feature is disabled when key is absent |
| Whitespace-only key | Rejected — provides no cryptographic entropy |
| Placeholder key (`YOUR_KEY_HERE`, `CHANGE_ME`, etc.) | Rejected — template values must be replaced |
| Key shorter than 32 characters | Rejected — minimum length per NIST SP 800-107 |
| Weak key (`password`, `12345678`, all-same-character string) | Rejected — insufficient entropy |

### Best Practices

- Never commit cryptographic keys to source control
- Use environment variables or secret managers (Azure Key Vault, AWS Secrets Manager) for production keys
- Use different keys per environment (development, staging, production)
- Rotate keys periodically according to your security policy
