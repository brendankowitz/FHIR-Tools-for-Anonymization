using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Shared.Core.UnitTests.Visitors
{
    /// <summary>
    /// Tests for AnonymizationVisitor handling of FHIR complex types and nested structures.
    /// </summary>
    public class AnonymizationVisitorComplexTypeTests : AnonymizationVisitorTestBase
    {
        [Fact]
        public void GivenHumanNameType_WhenRedacted_EntireNameShouldBeRemoved()
        {
            // Arrange
            var patient = CreateTestPatient();
            var config = CreateConfigurationWithRedaction("Patient.name");

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.Empty(anonymized.Name);
        }

        [Fact]
        public void GivenHumanNameType_WhenFamilyRedacted_OnlyFamilyShouldBeRemoved()
        {
            // Arrange
            var patient = CreateTestPatient();
            var config = CreateConfigurationWithRedaction("Patient.name.family");

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.NotEmpty(anonymized.Name);
            AssertRedacted(anonymized.Name[0].Family);
            Assert.NotEmpty(anonymized.Name[0].Given);
        }

        [Fact]
        public void GivenAddressType_WhenRedacted_EntireAddressShouldBeRemoved()
        {
            // Arrange
            var patient = CreateTestPatient();
            var config = CreateConfigurationWithRedaction("Patient.address");

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.Empty(anonymized.Address);
        }

        [Fact]
        public void GivenAddressType_WhenLineRedacted_OnlyLineShouldBeRemoved()
        {
            // Arrange
            var patient = CreateTestPatient();
            var config = CreateConfigurationWithRedaction("Patient.address.line");

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.NotEmpty(anonymized.Address);
            Assert.Empty(anonymized.Address[0].Line);
            AssertNotRedacted(anonymized.Address[0].City);
        }

        [Fact]
        public void GivenContactPointType_WhenValueRedacted_OnlyValueShouldBeRemoved()
        {
            // Arrange
            var patient = CreateTestPatient();
            var config = CreateConfigurationWithRedaction("Patient.telecom.value");

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.NotEmpty(anonymized.Telecom);
            AssertRedacted(anonymized.Telecom[0].Value);
            Assert.NotNull(anonymized.Telecom[0].System);
        }

        [Fact]
        public void GivenNestedComplexType_WhenParentRedacted_EntireStructureShouldBeRemoved()
        {
            // Arrange
            var patient = CreateTestPatient();
            var config = CreateConfigurationWithRedaction("Patient.address");

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.Empty(anonymized.Address);
        }

        [Fact]
        public void GivenCodeableConceptType_WhenTextRedacted_TextShouldBeNull()
        {
            // Arrange
            var observation = CreateTestObservation();
            var config = CreateConfigurationWithRedaction("Observation.code.text");

            // Act
            var anonymized = AnonymizeResource(observation, config);

            // Assert
            Assert.NotNull(anonymized.Code);
            AssertRedacted(anonymized.Code.Text);
        }

        [Fact]
        public void GivenResourceReferenceType_WhenReferenceRedacted_ReferenceShouldBeNull()
        {
            // Arrange
            var observation = CreateTestObservation();
            var config = CreateConfigurationWithRedaction("Observation.subject.reference");

            // Act
            var anonymized = AnonymizeResource(observation, config);

            // Assert
            Assert.NotNull(anonymized.Subject);
            AssertRedacted(anonymized.Subject.Reference);
        }

        [Fact]
        public void GivenMultipleComplexTypes_WhenSelectivelyRedacted_OnlySpecifiedFieldsShouldBeRemoved()
        {
            // Arrange
            var patient = CreateTestPatient();
            var config = CreateDefaultConfiguration();
            config.FhirPathRules.Add(CreateRule("Patient.name.family", "redact"));
            config.FhirPathRules.Add(CreateRule("Patient.address.line", "redact"));
            config.FhirPathRules.Add(CreateRule("Patient.telecom.value", "redact"));

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.NotEmpty(anonymized.Name);
            AssertRedacted(anonymized.Name[0].Family);
            Assert.NotEmpty(anonymized.Name[0].Given);
            
            Assert.NotEmpty(anonymized.Address);
            Assert.Empty(anonymized.Address[0].Line);
            AssertNotRedacted(anonymized.Address[0].City);
            
            Assert.NotEmpty(anonymized.Telecom);
            AssertRedacted(anonymized.Telecom[0].Value);
        }

        [Fact]
        public void GivenCollectionOfComplexTypes_WhenRedacted_AllItemsShouldBeProcessed()
        {
            // Arrange
            var patient = CreateTestPatient();
            // Add a second name
            patient.Name.Add(new HumanName
            {
                Family = "Jones",
                Given = new[] { "Jane" }
            });
            var config = CreateConfigurationWithRedaction("Patient.name.family");

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.Equal(2, anonymized.Name.Count);
            Assert.All(anonymized.Name, name => AssertRedacted(name.Family));
        }
    }
}
