using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Shared.Core.UnitTests.Visitors
{
    /// <summary>
    /// Tests for AnonymizationVisitor cryptographic hash functionality.
    /// </summary>
    public class AnonymizationVisitorCryptoHashTests : AnonymizationVisitorTestBase
    {
        [Fact]
        public void GivenAPatientWithName_WhenCryptoHashApplied_NameShouldBeHashed()
        {
            // Arrange
            var patient = CreateTestPatient();
            var originalName = patient.Name[0].Family;
            var config = CreateAnonymizerConfiguration("Patient.name.family", "cryptoHash");

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.NotNull(anonymized.Name[0].Family);
            Assert.NotEqual(originalName, anonymized.Name[0].Family);
            Assert.Matches(@"^[a-f0-9]{64}$", anonymized.Name[0].Family); // SHA-256 produces 64 hex chars
        }

        [Fact]
        public void GivenAPatient_WhenCryptoHashAppliedToId_IdShouldBeHashed()
        {
            // Arrange
            var patient = CreateTestPatient();
            var originalId = patient.Id;
            var config = CreateAnonymizerConfiguration("Patient.id", "cryptoHash");

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.NotNull(anonymized.Id);
            Assert.NotEqual(originalId, anonymized.Id);
            Assert.Matches(@"^[a-f0-9]{64}$", anonymized.Id);
        }

        [Fact]
        public void GivenSameInput_WhenCryptoHashAppliedMultipleTimes_ShouldProduceSameHash()
        {
            // Arrange
            var patient1 = CreateTestPatient();
            var patient2 = CreateTestPatient();
            var config = CreateAnonymizerConfiguration("Patient.name.family", "cryptoHash");

            // Act
            var anonymized1 = AnonymizeResource(patient1, config);
            var anonymized2 = AnonymizeResource(patient2, config);

            // Assert
            Assert.Equal(anonymized1.Name[0].Family, anonymized2.Name[0].Family);
        }

        [Fact]
        public void GivenAPatientWithTelecom_WhenCryptoHashApplied_TelecomShouldBeHashed()
        {
            // Arrange
            var patient = CreateTestPatient();
            var originalPhone = patient.Telecom[0].Value;
            var config = CreateAnonymizerConfiguration("Patient.telecom.value", "cryptoHash");

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.NotNull(anonymized.Telecom[0].Value);
            Assert.NotEqual(originalPhone, anonymized.Telecom[0].Value);
            Assert.Matches(@"^[a-f0-9]{64}$", anonymized.Telecom[0].Value);
        }

        [Fact]
        public void GivenMultipleFields_WhenCryptoHashApplied_AllFieldsShouldBeHashed()
        {
            // Arrange
            var patient = CreateTestPatient();
            var config = CreateDefaultConfiguration();
            config.FhirPathRules.Add(CreateRule("Patient.name.family", "cryptoHash"));
            config.FhirPathRules.Add(CreateRule("Patient.name.given", "cryptoHash"));

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.Matches(@"^[a-f0-9]{64}$", anonymized.Name[0].Family);
            Assert.All(anonymized.Name[0].Given, given => Assert.Matches(@"^[a-f0-9]{64}$", given));
        }
    }
}
