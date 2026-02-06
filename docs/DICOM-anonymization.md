# DICOM Data Anonymization

The Digital Imaging and Communication in Medicine (DICOM) standard has been commonly used for storing, viewing, and transmitting information in medical imaging. A DICOM file not only contains a viewable image but also a header with a large variety of data elements. These meta-data elements include identifiable information about the patient, the study, and the institution. Sharing such sensitive data demands proper protection to ensure data safety and maintain patient privacy. DICOM Anomymization Tool helps anonymize metadata in DICOM files for this purpose.

### Features
- Support anonymization methods for DICOM metadata including redact, keep, encrypt, cryptoHash, dateShift, perturb, substitute, remove and refreshUID.
- Configuration of the data elements that need to be anonymized.
- Configuration of the anonymization methods for each data element.
- Ability to run the tool on premise to anonymize a dataset locally.

### Build the solution
Use the .Net Core SDK to build DICOM Anonymization Tool. If you don't have .Net Core installed, instructions and download links are available [here](https://dotnet.microsoft.com/download/dotnet/6.0).

### Prepare DICOM Data
You can prepare your own DICOM files as input, or use sample DICOM files in folder $SOURCE\DICOM\samples of the project.

### Table of Contents

- [Anonymize DICOM data: using the command line tool](#anonymize-dicom-data-using-the-command-line-tool)
- [Customize configuration file](#customize-configuration-file)
- [Data anonymization algorithms](#data-anonymization-algorithms)
- [Output validation](#output-validation)


## Anonymize DICOM data: using the command line tool

Once you have built the command line tool, you will find executable file Microsoft.Health.Dicom.Anonymizer.CommandLineTool.exe in the $SOURCE\DICOM\src\Microsoft.Health.Dicom.Anonymizer.CommandLineTool\bin\Debug|Release\net8.0|net9.0|net10.0 folder.

You can use this executable file to anonymize DICOM file.

```
> .\Microsoft.Health.Dicom.Anonymizer.CommandLineTool.exe -i myInputFile -o myOutputFile
```

### Use Command Line Tool
The command-line tool can be used to anonymize one DICOM file or a folder containing DICOM files. Here are the parameters that the tool accepts:


| Option | Name | Optionality | Default | Description |
| ----- | ----- | ----- |----- |----- |
| -i | inputFile | Required (for file conversion) | | Input DICOM file. |
| -o | outputFile | Required (for file conversion) | |  Output DICOM file. |
| -c | configFile | Optional |configuration.json | Anonymizer configuration file path. It reads the default file from the current directory. |
| -I | inputFolder | Required (for folder conversion) |  | Input folder. |
| -O | outputFolder | Required (for folder conversion) |  | Output folder. |
| --validateInput | validateInput | Optional | false | Validate input DICOM file against value multiplicity, value types and format in [DICOM specification](http://dicom.nema.org/medical/Dicom/2017e/output/chtml/part06/chapter_6.html). |
| --validateOutput | validateOutput | Optional | false | Validate output DICOM file against value multiplicity, value types and format in [DICOM specification](http://dicom.nema.org/medical/Dicom/2017e/output/chtml/part06/chapter_6.html). |

> **[NOTE]**
> To anonymize one DICOM file, inputFile and outputFile are required. To anonymize a DICOM folder, inputFolder and outputFolder are required.

### Example use
```
> .\Microsoft.Health.Dicom.Anonymizer.CommandLineTool.exe -i myInputFile.dcm -o myOutputFile.dcm -c configuration.json
```

## Customize configuration file
Configuration file is important to describe the behavior of the anonymization. 

Here is a sample configuration file you can view the full version [here](../DICOM/src/Microsoft.Health.Dicom.Anonymizer.CommandLineTool/configuration.json):

```json
{
  "rules": [
    {
      "dicomTagPath": "PatientName",
      "method": "redact"
    },
    {
      "dicomTagPath": "PatientID",
      "method": "cryptoHash"
    },
    {
      "dicomTagPath": "StudyDate",
      "method": "dateShift"
    }
  ]
}
```

In the configuration file, you can define rules that specify anonymization methods for different DICOM tags.

### Configuration Parameters

- **dicomTagPath**: The path to the DICOM tag that needs to be anonymized. You can specify a standard DICOM keyword (e.g., "PatientName") or a tag number (e.g., "00100010").
- **method**: The anonymization method to apply to the DICOM tag. See [Data anonymization algorithms](#data-anonymization-algorithms) for available methods.

### Advanced Configuration

The configuration file also supports additional parameters for specific anonymization methods:

- **dateShiftKey**: Used with the dateShift method. If provided, should be at least 32 characters long.
- **cryptoHashKey**: Used with the cryptoHash method. If provided, should be at least 32 characters long.
- **encryptKey**: Used with the encrypt method. If provided, should be at least 32 characters long.

These keys should be added to a "parameters" section in the configuration file:

```json
{
  "rules": [
    {...}
  ],
  "parameters": {
    "dateShiftKey": "your-32-character-or-longer-key-here",
    "cryptoHashKey": "your-32-character-or-longer-key-here",
    "encryptKey": "your-32-character-or-longer-key-here"
  }
}
```

## Data anonymization algorithms

The following anonymization methods are supported for DICOM metadata:

| Anonymization Method | Method description |
| :------------- | :------------ |
| [Redact](#redact) | Removes the value of a DICOM tag.  |
| [Keep](#keep) | Retains the value as it is.  |
| [DateShift](#dateshift) | Shifts date/datetime fields within a specific range.  |
| [CryptoHash](#cryptohash) | Anonymizes identifiers using HMAC-SHA256.  |
| [Encrypt](#encrypt) | Replaces a field value with an encrypted string.  |
| [Substitute](#substitute) | Replaces the existing value with a specified value.  |
| [Perturb](#perturb) | Perturb numerical elements by random noise.  |
| [Remove](#remove) | Completely removes a DICOM tag from the file.  |
| [RefreshUID](#refreshuid) | Generates a new UID value while maintaining consistency.  |

### Redact
This method removes the value of a DICOM tag but keeps the tag in the file with an empty value.

**Example configuration:**
```json
{"dicomTagPath": "PatientName", "method": "redact"}
```

### Keep
Keep method retains the value that is present in the DICOM tag.

**Example configuration:**
```json
{"dicomTagPath": "Modality", "method": "keep"}
```

### DateShift
Shifts dates by a random amount while preserving temporal relationships within a patient's data. Requires a `dateShiftKey` in the parameters section.

**Example configuration:**
```json
{"dicomTagPath": "StudyDate", "method": "dateShift"}
```

### CryptoHash
Uses HMAC-SHA256 to anonymize identifiers. With a consistent `cryptoHashKey`, the same input always produces the same hash output.

**Example configuration:**
```json
{"dicomTagPath": "PatientID", "method": "cryptoHash"}
```

### Encrypt
Encrypts values using AES encryption. Requires an `encryptKey` in the parameters section.

**Example configuration:**
```json
{"dicomTagPath": "PatientID", "method": "encrypt"}
```

### Substitute
Replaces the existing value with a specified value provided in the configuration.

**Example configuration:**
```json
{"dicomTagPath": "PatientName", "method": "substitute", "replaceWith": "Anonymous"}
```

### Perturb
Adds random noise to numerical values. Requires `span`, `rangeType`, and `roundTo` parameters.

**Example configuration:**
```json
{"dicomTagPath": "PatientWeight", "method": "perturb", "span": 10, "rangeType": "proportional", "roundTo": 2}
```

### Remove
Completely removes a DICOM tag from the file.

**Example configuration:**
```json
{"dicomTagPath": "InstitutionName", "method": "remove"}
```

### RefreshUID
Generates a new UID value while maintaining consistency across related DICOM files.

**Example configuration:**
```json
{"dicomTagPath": "StudyInstanceUID", "method": "refreshUID"}
```

## Output validation

The tool can validate DICOM files before and after anonymization to ensure they conform to the DICOM standard. Use the `--validateInput` and `--validateOutput` flags to enable validation.

Validation checks:
- Value multiplicity (VM)
- Value representation (VR)
- Value format according to DICOM specification

**Example with validation:**
```
> .\Microsoft.Health.Dicom.Anonymizer.CommandLineTool.exe -i input.dcm -o output.dcm --validateInput --validateOutput
```

## Performance considerations

DICOM anonymization performance depends on several factors:
- File size and number of tags
- Complexity of anonymization rules
- Whether validation is enabled
- I/O performance of storage

For batch processing of many files, consider:
- Processing files in parallel when possible
- Disabling validation for production runs (after testing)
- Using fast storage (SSD) for input/output

## Frequently asked questions

### Does this tool anonymize pixel data?
No, this tool only anonymizes DICOM metadata. Pixel data anonymization (e.g., removing burned-in PHI from images) requires specialized imaging tools.

### Can I use custom DICOM tags?
Yes, you can specify custom tags using their tag numbers (e.g., "00101234").

### What happens if I don't provide encryption keys?
Some methods (encrypt, cryptoHash, dateShift) work better with keys but can operate without them. Without keys, consistency across datasets may not be maintained.

### How do I verify the anonymization is correct?
Use the validation flags and review sample output files to ensure that:
- Sensitive tags have been properly anonymized
- Required tags are still present
- Files are valid DICOM format

### Can I anonymize a folder of DICOM files?
Yes, use the `-I` and `-O` flags to specify input and output folders:
```
> .\Microsoft.Health.Dicom.Anonymizer.CommandLineTool.exe -I inputFolder -O outputFolder
```
