using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Shared.Core.UnitTests.Visitors
{
    /// <summary>
    /// Tests for AnonymizationVisitor redaction functionality.
    /// </summary>
    public class AnonymizationVisitorRedactionTests : AnonymizationVisitorTestBase
    {
        [Fact]
        public void GivenAPatientWithName_WhenRedacted_NameShouldBeRemoved()
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
        public void GivenAPatientWithAddress_WhenRedacted_AddressShouldBeRemoved()
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
        public void GivenAPatient_WhenFamilyNameRedacted_FamilyNameShouldBeNull()
        {
            // Arrange
            var patient = CreateTestPatient();
            var config = CreateConfigurationWithRedaction("Patient.name.family");

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.NotEmpty(anonymized.Name);
            AssertRedacted(anonymized.Name[0].Family);
        }

        [Fact]
        public void GivenAPatient_WhenGivenNameRedacted_GivenNamesShouldBeEmpty()
        {
            // Arrange
            var patient = CreateTestPatient();
            var config = CreateConfigurationWithRedaction("Patient.name.given");

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.NotEmpty(anonymized.Name);
            Assert.Empty(anonymized.Name[0].Given);
        }

        [Fact]
        public void GivenAPatient_WhenIdRedacted_IdShouldBeNull()
        {
            // Arrange
            var patient = CreateTestPatient();
            var config = CreateConfigurationWithRedaction("Patient.id");

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            AssertRedacted(anonymized.Id);
        }

        [Fact]
        public void GivenAnObservation_WhenEffectiveDateRedacted_EffectiveShouldBeNull()
        {
            // Arrange
            var observation = CreateTestObservation();
            var config = CreateConfigurationWithRedaction("Observation.effective");

            // Act
            var anonymized = AnonymizeResource(observation, config);

            // Assert
            Assert.Null(anonymized.Effective);
        }

        [Fact]
        public void GivenMultipleRules_WhenRedacted_AllSpecifiedFieldsShouldBeRemoved()
        {
            // Arrange
            var patient = CreateTestPatient();
            var config = CreateDefaultConfiguration();
            config.FhirPathRules.Add(CreateRule("Patient.name", "redact"));
            config.FhirPathRules.Add(CreateRule("Patient.address", "redact"));
            config.FhirPathRules.Add(CreateRule("Patient.telecom", "redact"));

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.Empty(anonymized.Name);
            Assert.Empty(anonymized.Address);
            Assert.Empty(anonymized.Telecom);
        }

        [Fact]
        public void GivenAPatientWithTelecom_WhenTelecomValueRedacted_ValueShouldBeNull()
        {
            // Arrange
            var patient = CreateTestPatient();
            var config = CreateConfigurationWithRedaction("Patient.telecom.value");

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.NotEmpty(anonymized.Telecom);
            AssertRedacted(anonymized.Telecom[0].Value);
        }

        [Fact]
        public void GivenAPatient_WhenPostalCodeRedacted_PostalCodeShouldBeNull()
        {
            // Arrange
            var patient = CreateTestPatient();
            var config = CreateConfigurationWithRedaction("Patient.address.postalCode");

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.NotEmpty(anonymized.Address);
            AssertRedacted(anonymized.Address[0].PostalCode);
            AssertNotRedacted(anonymized.Address[0].City);
        }
    }
}
