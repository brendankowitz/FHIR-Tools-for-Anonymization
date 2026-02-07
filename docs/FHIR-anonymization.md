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

### Building the solution
Use the .Net Core SDK to build FHIR Tools for Anonymization. If you don't have .Net Core installed, instructions and download links are available [here](https://dotnet.microsoft.com/download).

### Get sample FHIR files
This repo contains a few [sample](../FHIR/samples/) FHIR files that you can download. These files were generated using  [Synthea&trade; Patient Generator](https://github.com/synthetichealth/synthea). 

You can also export FHIR resource from your FHIR server using [Bulk Export](https://docs.microsoft.com/en-us/azure/healthcare-apis/configure-export-data).

### Configuration Files

This project includes multiple anonymization configuration files tailored to different compliance frameworks:

#### HIPAA Safe Harbor (configuration-sample.json)
The default configuration based on the [HIPAA Safe Harbor](https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html#safeharborguidance) method. This configuration removes or redacts 18 specific identifier types to achieve de-identification under US HIPAA regulations.

**Characteristics:**
- Primarily redaction-based
- Removes dates or reduces to year-only
- Full anonymization (not reversible)
- Lower research utility, higher privacy
- Suitable for HIPAA compliance in the US

#### GDPR Article 89 (configuration-gdpr-article89.json)
A configuration aligned with **GDPR Article 89(1)** for scientific research, archiving, and statistical purposes. This configuration implements pseudonymization to balance privacy protection with research utility.

**Characteristics:**
- Pseudonymization-focused (cryptographic hashing)
- Consistent date shifting preserves temporal relationships
- Maintains clinical data values
- Requires secure key management
- Higher research utility
- Suitable for EU GDPR compliance under Article 89

**⚠️ Important:** The GDPR Article 89 configuration produces **pseudonymized** data, not fully anonymized data. Cryptographic keys must be securely generated and protected. See [GDPR Article 89 Configuration Guide](GDPR-Article89-configuration.md) for detailed usage instructions.

### Table of Contents

- [Anonymize FHIR data: using the command line tool](#anonymize-fhir-data-using-the-command-line-tool)
- [Anonymize FHIR data: using Azure Data Factory](#anonymize-fhir-data-using-azure-data-factory)
- [Sample configuration file](#sample-configuration-file)
- [Sample rules using FHIR Path](#sample-rules-using-fhir-path)
- [Data anonymization algorithms](#data-anonymization-algorithms)

## Anonymize FHIR data: using the command line tool
Once you have built the command line tool, you will find two executable files for R4 and STU3 respectively: 

1. Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool.exe in the $SOURCE\FHIR\src\Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool\bin\Debug|Release\net8.0 (or net9.0, net10.0) folder. 

2. Microsoft.Health.Fhir.Anonymizer.Stu3.CommandLineTool.exe in the $SOURCE\FHIR\src\Microsoft.Health.Fhir.Anonymizer.Stu3.CommandLineTool\bin\Debug|Release\net8.0 (or net9.0, net10.0) folder.

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

Example using GDPR Article 89 configuration:
```
> .\Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool.exe -i myInputFolder -o myOutputFolder -c configuration-gdpr-article89.json
```

## Anonymize FHIR data: using Azure Data Factory

You can use the Azure PowerShell to create a Data Factory and a pipeline to anonymize FHIR data. The pipeline reads from an Azure blob container, anonymizes it as per the configuration file, and writes the output to another blob container. If you're new to Azure Data Factory, see [Introduction to Azure Data Factory](https://docs.microsoft.com/en-us/azure/data-factory/introduction).
