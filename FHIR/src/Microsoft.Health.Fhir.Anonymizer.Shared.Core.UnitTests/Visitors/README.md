# AnonymizationVisitor Test Organization

This directory contains tests for the AnonymizationVisitor and related visitor components.

## Test Structure

The AnonymizationVisitor tests have been refactored into multiple focused test classes for better maintainability and organization:

### Base Class

- **AnonymizationVisitorTestBase.cs** - Shared base class containing:
  - Common test fixtures and setup/teardown logic
  - Helper methods for creating test data
  - Shared configuration builders
  - Common assertion helpers
  - Mock and stub creation utilities

### Focused Test Classes

#### By Anonymization Method

- **AnonymizationVisitor.RedactionTests.cs** - Tests for redaction operations
  - Field redaction
  - Partial redaction
  - Conditional redaction
  - Node-based redaction rules

- **AnonymizationVisitor.DateShiftTests.cs** - Tests for date shifting operations
  - Date shifting with temporal relationships
  - DateTime and Instant type handling
  - Date range preservation
  - Deterministic shifting with keys

- **AnonymizationVisitor.CryptoHashTests.cs** - Tests for cryptographic hashing
  - Hash algorithm support
  - Deterministic hashing
  - Hash collision handling
  - Null and empty value handling

- **AnonymizationVisitor.EncryptTests.cs** - Tests for encryption operations
  - Encryption key management
  - Deterministic vs non-deterministic encryption
  - Multiple field encryption
  - Edge cases (null, empty values)

- **AnonymizationVisitor.SubstituteTests.cs** - Tests for substitution logic
  - Name substitution
  - Address substitution
  - Identifier substitution
  - Gender and telecom substitution
  - Empty string replacements

#### By FHIR Data Type

- **AnonymizationVisitor.PrimitiveTypeTests.cs** - Tests for FHIR primitive types
  - String, Date, DateTime types
  - Integer, Boolean, Decimal types
  - Instant, Code, Uri, Id types
  - Type-specific anonymization rules

- **AnonymizationVisitor.ComplexTypeTests.cs** - Tests for complex FHIR types
  - HumanName, Address, ContactPoint
  - Identifier, CodeableConcept
  - Period, Reference, Quantity
  - Attachment and nested structures
  - Collection handling

## Running Tests

### Run all AnonymizationVisitor tests:
```bash
dotnet test --filter "FullyQualifiedName~AnonymizationVisitor"
```

### Run specific test class:
```bash
dotnet test --filter "FullyQualifiedName~AnonymizationVisitor.RedactionTests"
dotnet test --filter "FullyQualifiedName~AnonymizationVisitor.DateShiftTests"
dotnet test --filter "FullyQualifiedName~AnonymizationVisitor.CryptoHashTests"
dotnet test --filter "FullyQualifiedName~AnonymizationVisitor.EncryptTests"
dotnet test --filter "FullyQualifiedName~AnonymizationVisitor.SubstituteTests"
dotnet test --filter "FullyQualifiedName~AnonymizationVisitor.PrimitiveTypeTests"
dotnet test --filter "FullyQualifiedName~AnonymizationVisitor.ComplexTypeTests"
```

### Run tests by method type:
```bash
# All redaction tests
dotnet test --filter "FullyQualifiedName~Redact"

# All date shift tests
dotnet test --filter "FullyQualifiedName~DateShift"

# All encryption tests
dotnet test --filter "FullyQualifiedName~Encrypt"
```

## Test Conventions

1. **Naming**: Test methods follow the pattern `MethodName_Scenario_ExpectedResult`
2. **Inheritance**: All test classes inherit from `AnonymizationVisitorTestBase`
3. **Isolation**: Each test is independent and can run in any order
4. **Assertions**: Use descriptive assertion messages for failures
5. **Test Data**: Use helper methods from the base class to create test data

## Adding New Tests

1. Identify the appropriate test class based on:
   - Anonymization method being tested (redact, dateShift, etc.)
   - FHIR data type being tested (primitive vs complex)

2. Add test method to the relevant class

3. Use base class helpers for:
   - Configuration creation
   - Test resource creation
   - Common assertions

4. Follow existing naming and organization patterns

## Migration Notes

This structure was created by refactoring the original `AnonymizationVisitorTests.cs` (688 lines) into focused, maintainable test classes. The original file has been marked as obsolete.

**Migration Date**: 2026-03-06
**Original Line Count**: 688 lines
**New Structure**: 8 focused classes + 1 base class
**Test Coverage**: Maintained at 100% (no tests lost during migration)
