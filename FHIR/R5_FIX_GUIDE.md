# R5 API Compatibility Fix Guide

## Overview
This document provides specific guidance on fixing the 20 compilation errors preventing R5 projects from building. The errors are due to API changes in Hl7.Fhir.R5 v6.0.2 compared to earlier versions.

## Error Categories and Solutions

### 1. SDK0001: ToTypedElement() Evaluation Warning (5 errors)

**Error Message:**
```
'Hl7.Fhir.ElementModel.VersionedConversionExtensions.ToTypedElement(Hl7.Fhir.Model.Base, string?)' 
is for evaluation purposes only and is subject to change or removal in future updates.
```

**Affected Files:**
- `EmptyElement.cs` (line 39, 68)
- `AnonymizerEngine.cs` (line 96)
- `SubstituteProcessor.cs` (line 45)
- `ResourceProcessor.cs` (line 124)

**Solution Options:**

#### Option A: Suppress the Warning
Add to each R5 project file:
```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);SDK0001</NoWarn>
</PropertyGroup>
```

#### Option B: Use Alternative API (Recommended)
Replace `.ToTypedElement()` calls with the stable API:
```csharp
// Old (R4/STU3):
var typedElement = resource.ToTypedElement();

// New (R5):
var typedElement = resource.ToTypedElement(ModelInfo.ModelInspector);
```

### 2. Constants.SupportedVersion Missing (2 errors)

**Error Message:**
```
'Constants' does not contain a definition for 'SupportedVersion'
```

**Affected File:**
- `AnonymizerConfigurationValidator.cs` (line 23, 25)

**Root Cause:**
The `SupportedVersion` constant was removed or renamed in Hl7.Fhir.R5.

**Solution:**

#### Investigate R5 API:
```bash
# Check what constants are available in R5
dotnet new console -n R5Test
cd R5Test
dotnet add package Hl7.Fhir.R5 -v 6.0.2
# Create test file to explore Constants class
```

#### Implement Version-Specific Code:
```csharp
#if FHIR_R5
    private static readonly string SupportedVersion = "5.0.0"; // R5 version string
#elif FHIR_R4
    private static readonly string SupportedVersion = Constants.SupportedVersion;
#else // STU3
    private static readonly string SupportedVersion = Constants.SupportedVersion;
#endif
```

### 3. FhirJsonSerializationSettings Missing (2 errors)

**Error Message:**
```
The type or namespace name 'FhirJsonSerializationSettings' could not be found
```

**Affected File:**
- `AnonymizerEngine.cs` (line 109)

**Root Cause:**
Serialization settings have changed in R5. The API now uses different classes.

**Solution:**

#### Check R5 Serialization API:
Look for replacement in:
- `Hl7.Fhir.Serialization.FhirJsonPocoSerializerSettings`
- `Hl7.Fhir.Serialization.SerializerSettings`

#### Implement Version-Specific Code:
```csharp
#if FHIR_R5
    // Use new R5 serialization settings
    var settings = new Hl7.Fhir.Serialization.FhirJsonPocoSerializerSettings();
#else
    // Use old serialization settings
    var settings = new FhirJsonSerializationSettings();
#endif
```

### 4. DotNetAttributeValidation Missing (1 error)

**Error Message:**
```
The name 'DotNetAttributeValidation' does not exist in the current context
```

**Affected File:**
- `AttributeValidator.cs` (line 13)

**Root Cause:**
Validation API has changed in R5.

**Solution:**

#### Investigate R5 Validation:
```csharp
// Check available validation methods in R5
// Look in Hl7.Fhir.Specification namespace
using Hl7.Fhir.Specification;
using Hl7.Fhir.Validation;
```

#### Possible Replacement:
```csharp
#if FHIR_R5
    // Use new R5 validation API
    var validator = new Validator(new ValidationSettings());
#else
    // Use old validation API
    DotNetAttributeValidation.Validate(resource);
#endif
```

### 5. s_quantityTypeNames Missing (1 error)

**Error Message:**
```
The name 's_quantityTypeNames' does not exist in the current context
```

**Affected File:**
- `PerturbProcessor.cs` (line 44)

**Root Cause:**
This is likely a missing static field initialization in the shared code.

**Solution:**

#### Check if it's defined elsewhere:
```bash
cd /home/runner/work/FHIR-Tools-for-Anonymization/FHIR-Tools-for-Anonymization/FHIR
grep -r "s_quantityTypeNames" src/
```

#### If it needs to be version-specific:
```csharp
#if FHIR_R5
    private static readonly HashSet<string> s_quantityTypeNames = new HashSet<string>
    {
        "Quantity", "Age", "Distance", "Duration", "Count", "MoneyQuantity", "SimpleQuantity"
    };
#elif FHIR_R4
    private static readonly HashSet<string> s_quantityTypeNames = new HashSet<string>
    {
        "Quantity", "Age", "Distance", "Duration", "Count", "MoneyQuantity", "SimpleQuantity"
    };
#else // STU3
    private static readonly HashSet<string> s_quantityTypeNames = new HashSet<string>
    {
        "Quantity", "Age", "Distance", "Duration", "Count", "SimpleQuantity"
    };
#endif
```

## Implementation Steps

### Step 1: Add Conditional Compilation Symbols

Update each R5 project file to define the `FHIR_R5` symbol:

**R5.Core.csproj:**
```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);FHIR_R5</DefineConstants>
</PropertyGroup>
```

Similarly for R4 and STU3:
- R4: Add `<DefineConstants>$(DefineConstants);FHIR_R4</DefineConstants>`
- STU3: Add `<DefineConstants>$(DefineConstants);FHIR_STU3</DefineConstants>`

### Step 2: Create Version-Specific Adapter Classes

Create adapter classes to abstract version differences:

**IFhirVersionAdapter.cs** (in Shared.Core):
```csharp
public interface IFhirVersionAdapter
{
    ITypedElement ToTypedElement(Base resource);
    string GetSupportedVersion();
    object GetSerializationSettings();
    void ValidateResource(Resource resource);
}
```

**FhirR5Adapter.cs** (R5-specific file, not in shared):
```csharp
public class FhirR5Adapter : IFhirVersionAdapter
{
    public ITypedElement ToTypedElement(Base resource)
    {
        return resource.ToTypedElement(ModelInfo.ModelInspector);
    }
    
    public string GetSupportedVersion() => "5.0.0";
    
    // Implement other methods...
}
```

### Step 3: Research R5 API Documentation

Consult these resources:
1. **Hl7.Fhir.R5 NuGet Package Documentation**: https://www.nuget.org/packages/Hl7.Fhir.R5/
2. **Breaking Changes Documentation**: Check the release notes for v6.0.x
3. **FHIR .NET API GitHub**: https://github.com/FirelyTeam/firely-net-sdk
4. **Migration Guide**: Look for FHIR SDK migration guides from v5 to v6

### Step 4: Run Investigation Queries

```bash
# Check what's available in R5
cd FHIR/src/Microsoft.Health.Fhir.Anonymizer.R5.Core
dotnet build -v detailed 2>&1 | grep "error CS" | sort | uniq

# Get more details on specific types
dotnet list package --include-transitive | grep Hl7.Fhir
```

### Step 5: Fix and Test Incrementally

1. Fix one error category at a time
2. Build R5 projects after each fix
3. Verify R4 and STU3 still build correctly
4. Run unit tests for all versions

### Step 6: Test R5 Functionality

After compilation succeeds:
```bash
# Run R5 unit tests
dotnet test src/Microsoft.Health.Fhir.Anonymizer.R5.Core.UnitTests/

# Run R5 functional tests
dotnet test src/Microsoft.Health.Fhir.Anonymizer.R5.FunctionalTests/

# Test command line tool
dotnet run --project src/Microsoft.Health.Fhir.Anonymizer.R5.CommandLineTool/ -- --help
```

## Expected Timeline

- **Research Phase**: 2-4 hours
  - Investigate R5 API changes
  - Document all breaking changes
  
- **Implementation Phase**: 4-8 hours
  - Implement conditional compilation or adapter pattern
  - Fix all compilation errors
  - Update unit tests if needed
  
- **Testing Phase**: 2-4 hours
  - Run all unit tests (R5, R4, STU3)
  - Run functional tests
  - Manual testing of CLI tools

**Total Estimated Time**: 8-16 hours

## Success Criteria

- [ ] All R5 projects build without errors
- [ ] All R5 projects build without warnings (except expected SDK0001 if suppressed)
- [ ] R4 and STU3 projects still build successfully
- [ ] All unit tests pass for R5, R4, and STU3
- [ ] R5.CommandLineTool can anonymize sample R5 resources
- [ ] Code changes are well-documented with comments explaining version differences

## Additional Resources

### Useful NuGet Packages for Testing
```bash
dotnet add package Hl7.Fhir.R5 --version 6.0.2
dotnet add package Hl7.Fhir.Specification.R5 --version 6.0.2
```

### Code Analysis Tools
```bash
# Install dotnet-outdated to check for package updates
dotnet tool install --global dotnet-outdated-tool

# Run in FHIR directory
dotnet outdated
```

## Contact Points

If you encounter issues not covered here:
1. Check Firely .NET SDK GitHub Issues: https://github.com/FirelyTeam/firely-net-sdk/issues
2. Review FHIR R5 specification changes: http://hl7.org/fhir/R5/
3. Consult the FHIR community: https://chat.fhir.org/

## Notes

- The SDK0001 warning for `ToTypedElement()` might be safe to suppress as it's marked for evaluation but is widely used
- Some APIs might have been renamed rather than removed - check for similar method names
- R5 is a relatively new version, so expect some breaking changes from R4
- Consider creating a compatibility layer rather than using conditional compilation everywhere
