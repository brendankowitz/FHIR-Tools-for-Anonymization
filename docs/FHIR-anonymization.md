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

## Anonymize FHIR data: using Azure Data Factory

You can use the Azure PowerShell to create a Data Factory and a pipeline to anonymize FHIR data. The pipeline reads from an Azure blob container, anonymizes it as per the configuration file, and writes the output to another blob container. If you're new to Azure Data Factory, see [Introduction to Azure Data Factory](https://docs.microsoft.com/en-us/azure/data-factory/introduction).

* Use a PowerShell script to create a data factory pipeline.
* Trigger on-demand pipeline run.
* Monitor the pipeline and activity runs.

### Prerequisites

* **Azure subscription**: If you don't have an Azure subscription, create a [free account](https://azure.microsoft.com/free/) before you begin.
* **Azure storage account**: Azure Blob storage is used as the _source_ & _destination_ data store. If you don't have an Azure storage account, see the instructions in [Create a storage account](https://docs.microsoft.com/en-us/azure/storage/common/storage-account-create?tabs=azure-portal). 
* **Azure PowerShell**: Azure PowerShell is used for deploying azure resources. If you don't have Azure PowerShell installed, see the instructions in [Install the Azure PowerShell module](https://docs.microsoft.com/en-us/powershell/azure/install-az-ps?view=azps-3.4.0)
* **.Net Core**: Use .Net Core sdk to build FHIR Tools for Anonymization. If you don't have .Net Core installed, instructions and download links are available [here](https://dotnet.microsoft.com/download).

#### Prepare azure storage resource container

Create a source and a destination container on your blob store. Upload your FHIR files to the source blob container. The pipeline will read the files from the source container and upload the anonymized files to the destination container.

You can also export FHIR resources from a FHIR server using [Bulk Export](https://github.com/microsoft/fhir-server/blob/master/docs/BulkExport.md) and put the data to the source blob container.

#### Log in to Azure using PowerShell
1. Launch **PowerShell** on your machine. Keep PowerShell open until the end of this tutorial. If you close and reopen, you need to run these commands again.

2. Run the following command, and enter the Azure user name and password to sign in to the Azure portal:

    ```powershell
    Connect-AzAccount
    ```

3. Run the following command to view all the subscriptions for this account:

    ```powershell
    Get-AzSubscription
    ```

4. If you see multiple subscriptions associated with your account, run the following command to select the subscription that you want to work with. Replace **SubscriptionId** with the ID of your Azure subscription:

    ```powershell
    Select-AzSubscription -SubscriptionId "<SubscriptionId>"
    ```

#### Build anonymization binaries using .NET Core SDK

Run the following commands in PowerShell to build the FHIR Tools for Anonymization binaries: 
```powershell
git clone https://github.com/microsoft/FHIR-Tools-for-Anonymization
cd FHIR-Tools-for-Anonymization/FHIR
dotnet build Fhir.Anonymizer.sln
```

#### Deploy a data factory using Azure PowerShell

1. Launch **PowerShell** on your machine.

2. Define variables for use in PowerShell commands later. Replace the placeholder values with your own values:

    ```powershell
    $subscriptionId = "<subscriptionId>"
    $resourceGroupName = "<resourceGroupName>"
    $region = "<region>"
    $dataFactoryName = "<dataFactoryName>"
    $sourceStorageAccountName = "<sourceStorageAccountName>"
    $sourceStorageAccountKey = "<sourceStorageAccountKey>"
    $sourceContainerName = "<sourceContainerName>"
    $destinationStorageAccountName = "<destinationStorageAccountName>"
    $destinationStorageAccountKey = "<destinationStorageAccountKey>"
    $destinationContainerName = "<destinationContainerName>"
    $activityContainerName = "<activityContainerName>"
    $blobPrefix = "<blobPrefix>"
    $blobFolder = "<blobFolder>"
    $configFileName = "<configFileName>"
    $toolVersion = "R4"  # Or use "Stu3"
    ```

3. Run the [CreateAzureDataFactoryPipeline.ps1](../FHIR/samples/AzureDataFactoryPipeline/scripts/CreateAzureDataFactoryPipeline.ps1) PowerShell script to create a data factory pipeline for anonymizing FHIR resources.

    ```powershell
    cd FHIR-Tools-for-Anonymization/FHIR/samples/AzureDataFactoryPipeline/scripts
    .\CreateAzureDataFactoryPipeline.ps1 -SubscriptionId $subscriptionId `
        -ResourceGroupName $resourceGroupName `
        -Region $region `
        -DataFactoryName $dataFactoryName `
        -SourceStorageAccountName $sourceStorageAccountName `
        -SourceStorageAccountKey $sourceStorageAccountKey `
        -SourceContainerName $sourceContainerName `
        -DestinationStorageAccountName $destinationStorageAccountName `
        -DestinationStorageAccountKey $destinationStorageAccountKey `
        -DestinationContainerName $destinationContainerName `
        -ActivityContainerName $activityContainerName `
        -BlobPrefix $blobPrefix `
        -BlobFolder $blobFolder `
        -ConfigFileName $configFileName `
        -ToolVersion $toolVersion
    ```

This script will create an Azure Data Factory pipeline named `FhirBulkDataAnonymization` which contains the activities:
* `LookupParametersActivity`: Lookup the parameters for anonymization from a CSV file stored in the Activity container.
* `AnonymizationActivity`: A ForEach activity which will anonymize each blob file in the source container in parallel.

#### Trigger a pipeline run

Navigate to the Azure portal, select your Data Factory resource, and then select **Author & Monitor** to open the Data Factory UI. Select the `FhirBulkDataAnonymization` pipeline, and then select **Add Trigger** > **Trigger Now**. You can also use the Azure PowerShell to trigger a pipeline run:

```powershell
$runId = Invoke-AzDataFactoryV2Pipeline `
    -ResourceGroupName $resourceGroupName `
    -DataFactoryName $dataFactoryName `
    -PipelineName "FhirBulkDataAnonymization"
```

#### Monitor the pipeline run

You can monitor the pipeline run in the Azure portal. In the Data Factory UI, select **Monitor** from the left menu. You see the pipeline run in the list. You can also use Azure PowerShell to monitor the pipeline run:

```powershell
Get-AzDataFactoryV2PipelineRun `
    -ResourceGroupName $resourceGroupName `
    -DataFactoryName $dataFactoryName `
    -PipelineRunId $runId
```

## How to perform de-identified $export operation on the FHIR Server

The de-identified export feature allows you to export de-identified data from the FHIR server without needing to set up any additional infrastructure or pipelines. This feature is available in the [Azure API for FHIR](https://docs.microsoft.com/en-us/azure/healthcare-apis/azure-api-for-fhir/overview) and the [FHIR server for Azure](https://github.com/microsoft/fhir-server).

To perform a de-identified export:

1. Call the standard FHIR [$export](http://hl7.org/fhir/uv/bulkdata/export/index.html) operation with the query parameter `_type=Patient` and an additional query parameter `_anonymizationConfig` which contains the URL of your anonymization configuration file, or `_anonymizationConfigCollectionReference` parameter to use a pre-configured anonymization template.

2. The anonymized data will be exported to your configured Azure blob storage account.

For more details, see the [De-identified export documentation](https://docs.microsoft.com/en-us/azure/healthcare-apis/azure-api-for-fhir/de-identified-export).

## Sample configuration file

[configuration-sample.json](../FHIR/src/configuration-sample.json) is a sample configuration file. The configuration file specifies how the anonymization engine should process FHIR resources. It consists of the following sections:

### FHIR Path rules

FHIR path rules define which elements should be anonymized and which anonymization method should be applied. Here is an example:

```json
"fhirPathRules": [
    {"path": "Resource.id", "method": "cryptoHash"},
    {"path": "nodesByType('Reference').reference", "method": "cryptoHash"},
    {"path": "Group.name", "method": "redact"},
    {"path": "Patient.address", "method": "redact"}
],
"parameters": {
    "dateShiftKey": "",
    "cryptoHashKey": "",
    "encryptKey": "",
    "enablePartialAgesForRedact": true,
    "enablePartialDatesForRedact": true,
    "enablePartialZipCodesForRedact": true,
    "restrictedZipCodeTabulationAreas": []
}
```

### Explanation of rules

* `fhirPathRules`: Defines a list of FHIR Path expressions and the corresponding anonymization method to apply.
  * `path`: A FHIR Path expression identifying the element(s) to anonymize.
  * `method`: The anonymization method to apply. See [Data anonymization algorithms](#data-anonymization-algorithms) for more details.

* `parameters`: Configuration parameters for the anonymization methods.
  * `dateShiftKey`: Key for the date-shift algorithm.
  * `cryptoHashKey`: Key for the cryptographic hash algorithm.
  * `encryptKey`: Key for the encryption algorithm.
  * `enablePartialAgesForRedact`: When true, ages over 89 are redacted to 90+ instead of being fully redacted.
  * `enablePartialDatesForRedact`: When true, dates are partially preserved (year only) instead of being fully redacted.
  * `enablePartialZipCodesForRedact`: When true, zip codes are partially preserved (first 3 digits) instead of being fully redacted.
  * `restrictedZipCodeTabulationAreas`: List of zip code tabulation areas (first 3 digits) that should be fully redacted.

## Sample rules using FHIR Path

The sample configuration file defines rules for redacting or hashing various FHIR elements. You can customize these rules based on your requirements. Here are some examples:

### Redact all patient names
```json
{"path": "Patient.name", "method": "redact"}
```

### Redact all addresses
```json
{"path": "nodesByType('Address')", "method": "redact"}
```

### Crypto-hash all IDs
```json
{"path": "Resource.id", "method": "cryptoHash"}
```

### Crypto-hash all references
```json
{"path": "nodesByType('Reference').reference", "method": "cryptoHash"}
```

### Date-shift all dates
```json
{"path": "nodesByType('date')", "method": "dateShift"}
```

## Data anonymization algorithms

The FHIR anonymizer provides several data anonymization algorithms that can be applied to FHIR elements:

|Method|Applicable FHIR types|Description|
|------|-----|-----|
|redact|All types|Removes the element from the resource.|
|keep|All types|Retains the element unchanged.|
|dateShift|Elements of type date, dateTime, and instant | Shifts the value using the [Date-shift algorithm](#date-shift).|
|cryptoHash|All types|Hashes the value using the [Crypto-hash algorithm](#crypto-hash).|
|encrypt|All types|Encrypts the value using the [Encryption algorithm](#encryption).|
|substitute|All types|Substitutes the value with a surrogate value.|
|perturb|Numerical types (integer, decimal, quantity, range, etc.)|Adds random noise to numerical values.|

### Crypto-hash

The crypto-hash algorithm uses HMAC-SHA256 to hash element values. The algorithm requires a key (`cryptoHashKey`). The same key should be used to hash the same value across different resources to maintain referential integrity.

**Key requirements:**
* Must be a base64-encoded 256-bit key
* Should be randomly generated and securely stored
* Should be consistent across all resources in a dataset to maintain referential integrity

**Use cases:**
* Hashing resource IDs
* Hashing references to maintain relationships between resources
* Hashing identifiers

**Example:** 
Original value: `Patient/123`
Hashed value: `Patient/8jR9fK3mN2pQ7sT1vW4xY6zA`

### Date-shift

The date-shift algorithm shifts dates by a random offset. The algorithm requires a key (`dateShiftKey`). The same key will generate the same offset for the same resource, ensuring that the relative time intervals between dates within a resource are preserved.

**Key requirements:**
* Must be a non-empty string
* Should be randomly generated and securely stored
* The offset is calculated per resource, so dates within the same resource are shifted by the same amount

**Use cases:**
* Shifting birth dates
* Shifting observation dates
* Shifting procedure dates

**Example:**
Original date: `2020-01-15`
Shifted date: `2020-03-22` (shifted by +67 days)

All dates within the same resource will be shifted by the same amount, preserving the time relationships.

### Encryption

The encryption algorithm encrypts element values using AES-256 encryption. The algorithm requires a key (`encryptKey`).

**Key requirements:**
* Must be a base64-encoded 256-bit key
* Should be randomly generated and securely stored
* The same key must be used to decrypt the data later

**Use cases:**
* Encrypting sensitive text fields that may need to be decrypted later
* Encrypting identifiers that need to be reversible

**Example:**
Original value: `John Doe`
Encrypted value: `AQIDBAUGBwgJCgsMDQ4PEBESExQ=`

### Redact

The redact algorithm removes the element from the resource entirely. This is the most aggressive anonymization method and ensures that the data is completely removed.

**Configuration options:**
* `enablePartialAgesForRedact`: When enabled, ages over 89 are redacted to "90+" instead of being removed
* `enablePartialDatesForRedact`: When enabled, dates are reduced to year-only precision instead of being removed
* `enablePartialZipCodesForRedact`: When enabled, zip codes are reduced to the first 3 digits instead of being removed
* `restrictedZipCodeTabulationAreas`: List of 3-digit zip code prefixes that should always be fully redacted (used for small population areas)

**Use cases:**
* Removing highly sensitive fields
* Removing identifiable information
* Complying with HIPAA Safe Harbor requirements

### Substitute

The substitute algorithm replaces the original value with a surrogate value. The surrogate value is generated based on the original value and a hash key, ensuring that the same input always produces the same output.

**Use cases:**
* Replacing real names with pseudonyms
* Replacing cities with representative cities
* Maintaining statistical properties while anonymizing

**Example:**
Original name: `John Doe`
Substitute name: `Jane Smith`

### Perturb

The perturb algorithm adds random noise to numerical values. The noise is calculated based on a percentage range of the original value.

**Configuration:**
* The noise range is specified as a percentage (default: ±10%)
* The perturbed value stays within the data type's valid range

**Use cases:**
* Adding noise to lab values
* Perturbing vital signs measurements
* Anonymizing age while keeping it approximately correct

**Example:**
Original value: `100 mg/dL`
Perturbed value: `105 mg/dL` (noise: +5%)

## HIPAA Safe Harbor Method

The sample configuration file is designed to comply with the HIPAA Safe Harbor method. The Safe Harbor method requires the removal of 18 types of identifiers:

1. Names
2. Geographic subdivisions smaller than a state (except first 3 digits of zip code if area has >20,000 people)
3. Dates (except year) directly related to an individual (birth, admission, discharge, death, age if over 89)
4. Telephone numbers
5. Fax numbers
6. Email addresses
7. Social Security numbers
8. Medical record numbers
9. Health plan beneficiary numbers
10. Account numbers
11. Certificate/license numbers
12. Vehicle identifiers and serial numbers
13. Device identifiers and serial numbers
14. Web URLs
15. IP addresses
16. Biometric identifiers
17. Full-face photographs
18. Any other unique identifying number, characteristic, or code

The sample configuration addresses these requirements through appropriate FHIR Path rules and anonymization methods. Organizations should review and customize the configuration based on their specific compliance requirements.