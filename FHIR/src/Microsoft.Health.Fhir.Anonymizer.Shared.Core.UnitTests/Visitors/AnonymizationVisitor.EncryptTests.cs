using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Shared.Core.UnitTests.Visitors
{
    /// <summary>
    /// Tests for AnonymizationVisitor encryption functionality.
    /// </summary>
    public class AnonymizationVisitorEncryptTests : AnonymizationVisitorTestBase
    {
        [Fact]
        public void GivenAPatientWithName_WhenEncrypted_NameShouldBeEncrypted()
        {
            // Arrange
            var patient = CreateTestPatient();
            var originalName = patient.Name[0].Family;
            var config = CreateConfigurationWithEncrypt("Patient.name.family", TestEncryptKey);

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.NotNull(anonymized.Name[0].Family);
            Assert.NotEqual(originalName, anonymized.Name[0].Family);
        }

        [Fact]
        public void GivenAPatient_WhenIdEncrypted_IdShouldBeEncrypted()
        {
            // Arrange
            var patient = CreateTestPatient();
            var originalId = patient.Id;
            var config = CreateConfigurationWithEncrypt("Patient.id", TestEncryptKey);

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.NotNull(anonymized.Id);
            Assert.NotEqual(originalId, anonymized.Id);
        }

        [Fact]
        public void GivenAPatientWithTelecom_WhenEncrypted_TelecomValueShouldBeEncrypted()
        {
            // Arrange
            var patient = CreateTestPatient();
            var originalPhone = patient.Telecom[0].Value;
            var config = CreateConfigurationWithEncrypt("Patient.telecom.value", TestEncryptKey);

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.NotNull(anonymized.Telecom[0].Value);
            Assert.NotEqual(originalPhone, anonymized.Telecom[0].Value);
        }

        [Fact]
        public void GivenSameInput_WhenEncryptedWithSameKey_ShouldProduceSameOutput()
        {
            // Arrange
            var patient1 = CreateTestPatient();
            var patient2 = CreateTestPatient();
            var config = CreateConfigurationWithEncrypt("Patient.name.family", TestEncryptKey);

            // Act
            var anonymized1 = AnonymizeResource(patient1, config);
            var anonymized2 = AnonymizeResource(patient2, config);

            // Assert
            Assert.Equal(anonymized1.Name[0].Family, anonymized2.Name[0].Family);
        }

        [Fact]
        public void GivenSameInput_WhenEncryptedWithDifferentKeys_ShouldProduceDifferentOutput()
        {
            // Arrange
            var patient1 = CreateTestPatient();
            var patient2 = CreateTestPatient();
            var config1 = CreateConfigurationWithEncrypt("Patient.name.family", "key1");
            var config2 = CreateConfigurationWithEncrypt("Patient.name.family", "key2");

            // Act
            var anonymized1 = AnonymizeResource(patient1, config1);
            var anonymized2 = AnonymizeResource(patient2, config2);

            // Assert
            Assert.NotEqual(anonymized1.Name[0].Family, anonymized2.Name[0].Family);
        }

        [Fact]
        public void GivenMultipleFields_WhenEncrypted_AllFieldsShouldBeEncrypted()
        {
            // Arrange
            var patient = CreateTestPatient();
            var originalFamily = patient.Name[0].Family;
            var originalGiven = patient.Name[0].Given.First();
            var config = CreateDefaultConfiguration();
            config.FhirPathRules.Add(CreateRule("Patient.name.family", "encrypt",
                new Dictionary<string, object> { ["encryptKey"] = TestEncryptKey }));
            config.FhirPathRules.Add(CreateRule("Patient.name.given", "encrypt",
                new Dictionary<string, object> { ["encryptKey"] = TestEncryptKey }));

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.NotEqual(originalFamily, anonymized.Name[0].Family);
            Assert.NotEqual(originalGiven, anonymized.Name[0].Given.First());
        }
    }
}
