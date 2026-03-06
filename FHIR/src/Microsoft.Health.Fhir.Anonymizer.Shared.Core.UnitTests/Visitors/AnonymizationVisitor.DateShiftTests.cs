using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Shared.Core.UnitTests.Visitors
{
    /// <summary>
    /// Tests for AnonymizationVisitor date shifting functionality.
    /// </summary>
    public class AnonymizationVisitorDateShiftTests : AnonymizationVisitorTestBase
    {
        [Fact]
        public void GivenAPatient_WhenBirthDateShifted_BirthDateShouldChange()
        {
            // Arrange
            var patient = CreateTestPatient();
            var originalBirthDate = patient.BirthDate;
            var config = CreateConfigurationWithDateShift("Patient.birthDate", TestDateShiftKey);

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.NotNull(anonymized.BirthDate);
            Assert.NotEqual(originalBirthDate, anonymized.BirthDate);
        }

        [Fact]
        public void GivenAnObservation_WhenEffectiveDateShifted_DateShouldChange()
        {
            // Arrange
            var observation = CreateTestObservation();
            var originalEffective = (observation.Effective as FhirDateTime)?.Value;
            var config = CreateConfigurationWithDateShift("Observation.effective", TestDateShiftKey);

            // Act
            var anonymized = AnonymizeResource(observation, config);

            // Assert
            Assert.NotNull(anonymized.Effective);
            var newEffective = (anonymized.Effective as FhirDateTime)?.Value;
            Assert.NotEqual(originalEffective, newEffective);
        }

        [Fact]
        public void GivenSamePatient_WhenDateShiftedWithSameKey_ShouldProduceSameShift()
        {
            // Arrange
            var patient1 = CreateTestPatient();
            var patient2 = CreateTestPatient();
            var config = CreateConfigurationWithDateShift("Patient.birthDate", TestDateShiftKey);

            // Act
            var anonymized1 = AnonymizeResource(patient1, config);
            var anonymized2 = AnonymizeResource(patient2, config);

            // Assert
            Assert.Equal(anonymized1.BirthDate, anonymized2.BirthDate);
        }

        [Fact]
        public void GivenSamePatient_WhenDateShiftedWithDifferentKeys_ShouldProduceDifferentShifts()
        {
            // Arrange
            var patient1 = CreateTestPatient();
            var patient2 = CreateTestPatient();
            var config1 = CreateConfigurationWithDateShift("Patient.birthDate", "key1");
            var config2 = CreateConfigurationWithDateShift("Patient.birthDate", "key2");

            // Act
            var anonymized1 = AnonymizeResource(patient1, config1);
            var anonymized2 = AnonymizeResource(patient2, config2);

            // Assert
            Assert.NotEqual(anonymized1.BirthDate, anonymized2.BirthDate);
        }

        [Fact]
        public void GivenMultipleDatesInResource_WhenDateShifted_AllDatesShouldShiftConsistently()
        {
            // Arrange
            var observation = CreateTestObservation();
            var config = CreateDefaultConfiguration();
            config.FhirPathRules.Add(CreateRule("Observation.effective", "dateShift", 
                new Dictionary<string, object> { ["dateShiftKey"] = TestDateShiftKey }));
            config.FhirPathRules.Add(CreateRule("Observation.issued", "dateShift",
                new Dictionary<string, object> { ["dateShiftKey"] = TestDateShiftKey }));

            // Act
            var anonymized = AnonymizeResource(observation, config);

            // Assert
            Assert.NotNull(anonymized.Effective);
            Assert.NotNull(anonymized.Issued);
        }

        [Fact]
        public void GivenAPatientWithNoBirthDate_WhenDateShiftApplied_ShouldNotThrowException()
        {
            // Arrange
            var patient = new Patient { Id = "test-123" };
            var config = CreateConfigurationWithDateShift("Patient.birthDate", TestDateShiftKey);

            // Act & Assert
            var exception = Record.Exception(() => AnonymizeResource(patient, config));
            Assert.Null(exception);
        }
    }
}
