# FHIR Data Anonymization

FHIR data anonymization is available in the following ways:

1. A command line tool. Can be used on-premises or in the cloud to anonymize data.
2. An Azure Data Factory (ADF) pipeline. Comes with a [script](#anonymize-fhir-data-using-azure-data-factory) to create a pipeline that reads data from Azure blob store and writes anonymized data back to a specified blob store.
3. [De-identified $export](#how-to-perform-de-identified-export-operation-on-the-fhir-server) operation in the [FHIR server for Azure](https://github.com/microsoft/fhir-server).

### Features
* Support anonymization of FHIR **STU3**, **R4**, and **R5** data in JSON as well as NDJSON format
* Configuration of the data elements that need to be anonymized 
* Configuration of the [anonymization methods](#fhir-path-rules) for each data element
* Ability to create a anonymization pipeline in Azure Data Factory
* Ability to run the tool on premise to anonymize a dataset locally

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
Once you have built the command line tool, you will find three executable files for STU3, R4, and R5 respectively: 

1. Microsoft.Health.Fhir.Anonymizer.Stu3.CommandLineTool.exe in the $SOURCE\FHIR\src\Microsoft.Health.Fhir.Anonymizer.Stu3.CommandLineTool\bin\Debug|Release\net8.0 folder.

2. Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool.exe in the $SOURCE\FHIR\src\Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool\bin\Debug|Release\net8.0 folder.

3. Microsoft.Health.Fhir.Anonymizer.R5.CommandLineTool.exe in the $SOURCE\FHIR\src\Microsoft.Health.Fhir.Anonymizer.R5.CommandLineTool\bin\Debug|Release\net8.0 folder.

 You can use these executables to anonymize FHIR resource files in a folder.   
```
> .\Microsoft.Health.Fhir.Anonymizer.<version>.CommandLineTool.exe -i myInputFolder -o myOutputFolder
```

Where `<version>` should be replaced with `Stu3`, `R4`, or `R5` depending on your FHIR version.

The command-line tool can be used to anonymize a folder containing FHIR resource files. Here are the parameters that the tool accepts:

| Option | Name | Optionality | Default | Description |
| ----- | ----- | ----- |----- |----- |
| -i | inputFolder | Required | | Folder to locate input resource files. |
| -o | outputFolder | Required | |  Folder to save anonymized resource files. |