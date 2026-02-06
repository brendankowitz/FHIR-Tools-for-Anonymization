# DICOM Data Anonymization

The Digital Imaging and Communication in Medicine (DICOM) standard has been commonly used for storing, viewing, and transmitting information in medical imaging. A DICOM file not only contains a viewable image but also a header with a large variety of data elements. These meta-data elements include identifiable information about the patient, the study, and the institution. Sharing such sensitive data demands proper protection to ensure data safety and maintain patient privacy. DICOM Anomymization Tool helps anonymize metadata in DICOM files for this purpose.

### Features
- Support anonymization methods for DICOM metadata including redact, keep, encrypt, cryptoHash, dateShift, perturb, substitute, remove and refreshUID.
- Configuration of the data elements that need to be anonymized.
- Configuration of the anonymization methods for each data element.
- Ability to run the tool on premise to anonymize a dataset locally.

### Build the solution
Use the .Net Core SDK to build DICOM Anonymization Tool. If you don't have .Net Core installed, instructions and download links are available [here](https://dotnet.microsoft.com/download).

### Prepare DICOM Data
You can prepare your own DICOM files as input, or use sample DICOM files in folder $SOURCE\DICOM\samples of the project.

### Table of Contents

- [Anonymize DICOM data: using the command line tool](#anonymize-dicom-data-using-the-command-line-tool)
- [Customize configuration file](#customize-configuration-file)
- [Data anonymization algorithms](#data-anonymization-algorithms)
- [Output validation](#output-validation)


## Anonymize DICOM data: using the command line tool

Once you have built the command line tool, you will find executable file Microsoft.Health.Dicom.Anonymizer.CommandLineTool.exe in the $SOURCE\DICOM\src\Microsoft.Health.Dicom.Anonymizer.CommandLineTool\bin\Debug|Release\net8.0 (or net9.0, net10.0) folder.

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

Example usage to anonymize DICOM files in a folder:
```
.\Microsoft.Health.Dicom.Anonymizer.CommandLineTool.exe -I myInputFolder -O myOutputFolder -c myConfigFile
```

## Sample configuration file
The configuration is specified in JSON format and has three required high-level sections. The first section named _rules_, it specifies anonymization methods for DICOM tag. The second and third sections are _defaultSettings_ and _customSettings_ which specify default settings and custom settings for anonymization methods respectively.

|Fields|Description|
|----|----||
|rules|Anonymization rules for tags.|
|defaultSettings|Default settings for anonymization functions. Default settings will be used if not specify settings in rules.|
|customSettings|Custom settings for anonymization functions.|


DICOM Anonymization tool comes with a sample configuration file to help meet the requirements of HIPAA Safe Harbor Method. DICOM standard also describes attributes within a DICOM dataset that may potentially result in leakage of individually identifiable information according to HIPAA Safe Harbor. Our tool will build in a sample [configuration file](../DICOM/src/Microsoft.Health.Dicom.Anonymizer.CommandLineTool/configuration.json) that covers [application level confidentiality profile attributes](http://dicom.nema.org/medical/dicom/2018e/output/chtml/part15/chapter_E.html) defined in DICOM standard.

## Customize configuration file

### How to set rules

Users can list anonymization rules for individual DICOM tag (by tag value or tag name) as well as a set of tags (by masked value or DICOM VR). Ex：
```
{
    "rules": [
            {"tag": "(0010,1010)","method": "perturb"}, 
            {"tag": "(0040,xxxx)",  "method": "redact"},
            {"tag": "PatientID",  "method": "cryptohash"},
            {"tag": "PN", "method": "encrypt"}
    ]
}
```
Parameters in each rule:

|Fields|Description| Valid Value|Required|default value|
|--|-----|-----|--|--|
|tag|Used to define DICOM elements |1. Tag Value, e.g. (0010, 0010) or 0010,0010 or 00100010. <br>2. Tag Name. e.g. PatientName. <br> 3. Masked DICOM Tag (see note) <br> 4. DICOM VR. e.g. PN, DA.|True|null| 
|method|anonymization method| keep, redact, perturb, dateshift, encrypt, cryptohash, substitute, refreshUID, remove.| True|null|
|setting| Setting for anonymization method. Users can add custom settings in the field of "customSettings" and specify setting's name here. |valid setting's name |False|Default setting in the field of "defaultSettings"|
|params|parameters override setting for anonymization methods.|valid parameters|False|null|

> Masked tags follow the [DICOM convention](https://dicom.nema.org/medical/dicom/current/output/chtml/part06/chapter_5.html). `x` in a group or element number, means any value from 0 through F inclusive.

Each DICOM tag can only be anonymized once, if two rules have conflicts on one tag, only the former rule will be applied.

### How to set settings
_defaultSettings_ and _customSettings_ are used to config anonymization method. (Detailed parameters are defined in [Anonymization algorithm](#data-anonymization-algorithms). _defaultSettings_ are used when user does not specify settings in rule. As for _customSettings_, users need to add the setting with unique name. This setting can be used in "rules" by name.

Here is an example, the first rule will use `perturb` setting in _defaultSettings_ and the second one will use `perturbCustomerSetting` in field _cutomSettings_.