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

1. Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool.exe in the $SOURCE\FHIR\src\Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool\bin\Debug|Release\net8.0|net9.0|net10.0 folder. 

2. Microsoft.Health.Fhir.Anonymizer.Stu3.CommandLineTool.exe in the $SOURCE\FHIR\src\Microsoft.Health.Fhir.Anonymizer.Stu3.CommandLineTool\bin\Debug|Release\net8.0|net9.0|net10.0 folder.

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
| -b | bulkData | Optional | false | Resource file is in NDJSON bulk data format. |
| -r | recursive | Optional | false | Process resource files in input folder recursively. |
| -v | verbose | Optional | false | Provide additional details during processing. |
| -p | validateInput | Optional | false | Validate input resource files.
| -q | validateOutput | Optional | false | Validate output resource files.
| -k | skipExistedFile | Optional | false | Skip existed output files. If not specified, existed output files would be overwritten.

### Notes on config file parameters

The following are the explanations of each parameter in the config file parameters table:

- **inputFolder**: Location of the resource files to anonymize.
- **outputFolder**: Location to save anonymized resource files.
- **configFile**: Configuration file describes how the data should be anonymized. The repo contains a [sample configuration file](../FHIR/src/sample-configuration.json) that is based on [HIPAA Safe Harbor](https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html#safeharborguidance) method. The configuration file format is described in more detail [here](#sample-configuration-file).
- **bulkData**: Flag to indicate whether the input files are in bulk data format. We assume that the files would be in JSON format if this flag is not specified.
- **recursive**: Flag to indicate if resource files in the input folder and its subfolders should all be anonymized.
- **validateInput**: Flag to validate input resource files before anonymization. The tool will not anonymize resource file if it fails to be validated.
- **validateOutput**: Flag to validate output resource files. If validation fails, partial anonymized file will not be written to output folder.
- **skipExistedFile**: Flag to indicate if existed file in output folder should be skipped. If not specified, existed output file would be overwritten.
- **verbose**: Flag to provide additional information during the anonymization process. 

### Example use
```
> .\Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool.exe -i myInputFolder -o myOutputFolder -c configuration-sample.json -v
```

## Anonymize FHIR data: using Azure Data Factory
We have created sample scripts to help set-up an anonymization pipeline in [Azure Data Factory](https://docs.microsoft.com/en-us/azure/data-factory/). You can locate the script [here](../FHIR/src/FhirAnonymizerBulkDataPipeline/).

Azure Data Factory is a data integration service that can ingest data from various sources, anonymize the data, and output the anonymized data into a destination data store of your choice. The sample script will create a Data Factory pipeline that reads resource files from an [Azure Blob store](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blobs-overview), anonymizes it, and then outputs the anonymized data to a destination Azure Blob store that you specify. Please see the [README](../FHIR/src/FhirAnonymizerBulkDataPipeline/readme.md) that describes how to use the sample scripts.

The diagram below illustrates how the sample anonymization pipeline works. The source data store contains FHIR resources in JSON or NDJSON format. The pipeline then processes data files from source and moves the anonymized data to the destination store.

![FHIR anonymization pipeline](../FHIR/docs/images/fhir-anonymization-architecture.png)

## Sample configuration file
Configuration file specifies how the anonymization should be performed. It specifies the version, FHIR version, and processing rules for different FHIR elements and data types.

Here is a sample configuration file. You can view the complete sample [here](../FHIR/src/sample-configuration.json):

```json
{
  "fhirVersion": "R4",
  "processingErrors": "raise",
  "fhirPathRules": [
    {"path": "Resource.id", "method": "cryptoHash"},
    {"path": "nodesByType('Extension')", "method": "redact"},
    {"path": "nodesByType('Narrative')", "method": "redact"}
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
}
```

### Explanation of configuration parameters

Here's an explanation of each parameter:

- **fhirVersion**: Indicates the FHIR version of the input resource file. Supported versions are STU3 and R4.
- **processingErrors**: Indicates how to handle de-identified result issues. Possible values are:
    - **raise**: Raise exceptions if issues are detected.
    - **keep**: Keep the detected issues and continue with the anonymization.
    - **skip**: Skip anonymizing elements that have issues.
- **fhirPathRules**: List of FHIR path rules for resource nodes.
- **parameters**: A set of parameters that are used by some anonymization methods.
  - **dateShiftKey**: The key for date shift. It's used to create randomized date shift values for each patient to preserve temporal relationships. If provided, 32 or more random characters should be used. If you do not want consistent date shifts and instead want to use random shift values every time, leave this field empty.
  - **cryptoHashKey**: The key for crypto-hash to anonymize patient-related identifiers using the HMAC-SHA256 cryptographic hash function. If provided, 32 or more random characters should be used.
  - **encryptKey**: The key for encryption. If provided, 32 or more random characters should be used.
  - **enablePartialAgesForRedact**: When redact method is used, replace ages over 89 with a code specifying the age is greater than 89.
  - **enablePartialDatesForRedact**: When redact method is used, preserve year for dates. Precision will be uniform for all date fields.
  - **enablePartialZipCodesForRedact**: When redact method is used, preserve leading three digits from zip codes (postal code). For restricted zip codes that cannot be preserved, they are redacted entirely.
  - **restrictedZipCodeTabulationAreas**: Specify zip codes (postal codes) that should be redacted per HIPAA Safe Harbor Method. Any zip code that refers to a region with a population less than 20000 should be included in this list. The [HHS guidelines for Safe Harbor de-identification](https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html#safeharborguidance) list all restricted zip codes that should be included.

### Sample rules using FHIR Path
Anonymization methods are mapped to FHIR elements using [FHIR Path](https://www.hl7.org/fhir/fhirpath.html). FHIR path is a path-based query language to select elements in a FHIR resource.

Here are a few examples that show how to apply anonymization methods to specific FHIR elements:

### Example 1. Redact all instances of a data type in all resources
```json
{"path": "nodesByType('HumanName')", "method": "redact"}
```
Here the FHIR path query selects all nodes that have the datatype [HumanName](https://www.hl7.org/fhir/datatypes.html#HumanName) anywhere in the resource. And the [redact](#redact) method will be applied to anonymize that element. 

### Example 2. Redact a specific element in a resource
```json
{"path": "Patient.address", "method": "redact"}
```
Here the FHIR path selects the address element of the [Patient](http://hl7.org/fhir/patient.html) resource. 

### Example 3. Substitute a specific element
```json
{"path": "Patient.contact.name", "method": "substitute", "replaceWith": "Anonymous"}
```
Here the FHIR path selects the name element of the contact in Patient resource. And the [substitute](#substitute) method replaces the existing value with the value provided in the replaceWith parameter, in this case "Anonymous". 

### Example 4. Perturb a specific element
```json
{"path": "Observation.value.ofType(Quantity).value", "method": "perturb", "span": 10, "rangeType": "proportional", "roundTo": 0}
```
Here the FHIR path selects the value element from the [Observation](http://hl7.org/fhir/observation.html) resource provided it is of type [Quantity](http://hl7.org/fhir/datatypes.html#Quantity). The [perturb](#perturb) method then jitters the value by +/- 10% ("span" indicates 10% variation when "rangeType" is proportional) and rounds the result to 0 decimal places.

### Example 5. Crypto-hash an element with a particular code
```json
{"path": "Observation.where(code.coding.system='http://loinc.org').where(code.coding.code='41919-6').value", "method": "cryptoHash"}
```
Here the FHIR path first filters for Observation resources based on a particular code from the LOINC system and then selects the value. The [cryptoHash](#cryptohash) method then replaces the existing value with a crypto-hash of the original value.

### Example 6. Keep an element as it is
```json
{"path": "Device.id", "method": "keep"}
```
Here the FHIR path selects the id element of the [Device](https://www.hl7.org/fhir/device.html) resource and keeps the values as is without any modification.

### Example 7. Crypto-hash all resource ids
```json
{"path": "Resource.id", "method": "cryptoHash"}
```
In this case, 'Resource' means all resource types. The method applies to all resource ids.

### Additional FHIR Path examples
For additional FHIR path query examples, please refer to the [FHIR Path specification](https://www.hl7.org/fhirpath/).

## Data anonymization algorithms

The configuration file and rule mapping described earlier describes how data anonymization algorithms/methods can be mapped to different FHIR elements. In this section, we go deeper into the methods that are provided. 

The following table lists different anonymization methods/algorithms that are available for FHIR data elements.

| Anonymization Method | Method description |
| :------------- | :------------ |
| [Redact](#redact) | Removes a data element.  |
| [Keep](#keep) | Retains the value as it is.  |
| [DateShift](#dateshift) | Shifts date/datetime fields within a specific range while preserving temporal relationships within a dataset.  |
| [CryptoHash](#cryptohash) | Anonymizes patient-related identifiers using the HMAC-SHA256 cryptographic hash function.  |
| [Encrypt](#encrypt) | Replaces a field value with an encrypted string.  |
| [Substitute](#substitute) | Replaces the existing value with the value provided in the configuration file.  |
| [Perturb](#perturb) | Perturb numerical elements by a random noise or round to a given base.  |
| [Generalize](#generalize) | Keeps the value at a higher level of abstraction or generalization of the original.  |

### Redact
This method removes a data element by replacing it with an empty object. Depending on the FHIR element data type, the behavior is slightly different. Following are some examples:

- **Primitive types**: The primitive value is replaced with an [absent reason extension](http://hl7.org/fhir/extension-data-absent-reason.html). For example, suppose the FHIR path selects a boolean type. In that case, the boolean is replaced with: <br>
  ```json
  { 
    "extension": [ 
      { 
        "url": "http://hl7.org/fhir/StructureDefinition/data-absent-reason", 
        "valueCode": "masked" 
      } 
    ] 
  }
  ```
- **Complex types**: The selected element and child values are removed. For example, if the selected element has a data type [ContactPoint](https://www.hl7.org/fhir/datatypes.html#ContactPoint), all properties (system, value, use, rank, and period) will be removed.
- **Reference type**: A reference is a complex data type. References are removed along with its child elements.

**Example configuration:**
```json
{"path": "nodesByType('HumanName')", "method": "redact"}
```

### Keep
Keep method retains the value that is present in the selected element.

**Example configuration:**
```json
{"path": "Resource.id", "method": "keep"}
```

### DateShift
[Date-shifting](https://www.hl7.org/fhir/security.html#dateShift) is a FHIR anonymization method that shifts dates within a specific range while preserving temporal relationships within a single dataset. The method shifts dates or datetime fields by a random amount of time. Dates from the same patient (using a Medical Record Number) are shifted by the same amount of time, thus preserving the temporal relationships. For each patient, the same date-shift amount is used across different input datasets, ensuring consistency across related data.

Additionally, you can provide the key `dateShiftKey` (at least 32-characters long) in the parameters section of the configuration file. This ensures that shifting is done in a cryptographically strong manner.

**Example configuration:**
```json
{"path": "nodesByType('date')", "method": "dateShift"}
```

### CryptoHash
This method uses HMAC-SHA256 cryptographic hash function to anonymize patient-related identifiers. If a key `cryptoHashKey` is provided, hashing is done in a way that the same value will produce the same hash. This is useful when multiple datasets (from the same data holder) need to be linked. The key `cryptoHashKey` should be 32 characters or more.

**Example configuration:**
```json
{"path": "Resource.id", "method": "cryptoHash"}
```

### Encrypt
This method encrypts values with AES encryption. To use this method, an encryption key `encryptKey` must be provided in the parameters section of the configuration file. The key must be 32 characters or more.

**Example configuration:**
```json
{"path": "Patient.identifier.value", "method": "encrypt"}
```

### Substitute
Substitute method replaces the existing value with the value provided in the replaceWith configuration parameter. Here's an example:

**Example configuration:**
```json
{"path": "Patient.contact.name", "method": "substitute", "replaceWith": "Anonymous"}
```

### Perturb
Perturb method is used to add noise or randomize numerical data. This method is typically used for numerical fields such as age, measurements, financial amounts, and so on. 

Here are the parameters required for this method:

- **span**: The range around the original value within which the perturbed value should fall.
- **rangeType**: Either `fixed` or `proportional`. In case of `fixed`, the perturbed value is the original value +/- a random value in the range [0, span]. In case of `proportional`, the perturbed value is the original value +/- a random value in the range [0, originalValue * span]. 
- **roundTo**: Number of decimal digits to round the perturbed result to. This helps ensure that the perturbed value is not easily identified as having been jittered.

**Example configuration (proportional):**
```json
{"path": "Observation.value.ofType(Quantity).value", "method": "perturb", "span": 0.1, "rangeType": "proportional", "roundTo": 0}
```
For proportional range type with a span of 0.1, a value of 100 will be perturbed to somewhere between 90 and 110.

**Example configuration (fixed):**
```json
{"path": "Observation.value.ofType(Quantity).value", "method": "perturb", "span": 10, "rangeType": "fixed", "roundTo": 2}
```
For fixed range type with a span of 10, a value of 100 will be perturbed to somewhere between 90 and 110. The result will be rounded to 2 decimal places.

### Generalize
Generalize method keeps the value at a higher level of abstraction or generalization. For example, you can generalize age to an age range, or generalize a date to just the year.

Currently, generalize supports the following cases:
- **dateTime**: You can generalize a dateTime type to any combination of year, month, and/or day by providing the "cases" parameter. 

**Example configuration:**
```json
{"path": "nodesByType('dateTime')", "method": "generalize", "cases": "year"}
```
If the original value is "1990-01-01T10:00:00", after generalization, the result would be "1990".

Available generalization options for dates are: 
- "year"
- "yearMonth" 
- "yearMonthDay"

If you need additional generalization capabilities, please open an issue on the GitHub repo.

## Anonymize DICOM imaging metadata
In addition to FHIR data, the tool also supports anonymizing DICOM imaging metadata. For details, see [DICOM anonymization](DICOM-anonymization.md).

## How to perform de-identified export operation on the FHIR server

The de-identified export feature is built in the FHIR Server for Azure. The feature allows you to export de-identified data from the FHIR server. When performing export operations, you can specify the anonymization configuration file URL, and the server will anonymize the exported data based on the specified configuration.

For more details on how to use the de-identified export operation, please refer to the [documentation](https://docs.microsoft.com/azure/healthcare-apis/fhir/de-identified-export).

## Data validation
FHIR Data Anonymization Tool can validate the input/output resource files against FHIR specification. During validation, the tool checks the following:

* Whether the resources are well-formed JSON.
* Whether the resources conform to the respective FHIR version specification.
* Whether the resources conform to declared profiles.

Data validation is turned off by default. You can enable input and output validation using the --validateInput and --validateOutput flags respectively in the command-line tool.

## FHIR Path engine
The anonymization engine uses FHIR Path to select nodes in the resource. Currently, the engine uses [Firely SDK](https://github.com/FirelyTeam/firely-net-sdk) FHIR Path implementation. For more details about FHIR path and the supported features, please refer to [FHIR Path specification](http://hl7.org/fhirpath/).

## Performance test

We performed an anonymization benchmark using [Synthea sample data](https://synthea.mitre.org/) with the [default configuration](../FHIR/src/sample-configuration.json) provided in this repo. Here's the result:

### Benchmark result

For a folder that contains 1000 FHIR bundles (JSON format, ~300 resources per bundle, total ~150 MB), the anonymization process (without validation) takes about 5 seconds on a test machine (AMD Ryzen 7 3700X, 32 GB RAM).

### Validation performance 

When you turn on the input or output validation flags, the processing time will increase. This is because the validation process requires parsing and validating each element in the resource against FHIR specification.

In the same test, when both input and output validation flags are turned on, the anonymization process takes about 45 seconds. When just one flag is set, it takes about 25 seconds.

## Frequently asked questions

### Can I use this tool with FHIR versions other than STU3 or R4?
Currently, the tool supports FHIR STU3 and R4. If you need support for other versions, please open an issue on the GitHub repo.

### I don't see an anonymization method that I need. What should I do?
If you need an anonymization method that is not currently provided, please open an issue on the GitHub repo. We also welcome contributions.

### How do I verify that the anonymization is working correctly?
You can review the anonymized output to verify that the selected elements have been properly anonymized. You can also use the validation flags to verify that the output resources are valid per FHIR specification.

### Can this tool anonymize FHIR data in formats other than JSON?
Currently, the tool supports JSON and NDJSON formats.

### I am getting an error. What should I do?
Please check that:
- Input files are valid FHIR resources in JSON or NDJSON format
- Configuration file is valid JSON and follows the expected schema
- Paths specified in configuration file are valid FHIR paths

If the problem persists, please open an issue on the GitHub repo with details about the error.
