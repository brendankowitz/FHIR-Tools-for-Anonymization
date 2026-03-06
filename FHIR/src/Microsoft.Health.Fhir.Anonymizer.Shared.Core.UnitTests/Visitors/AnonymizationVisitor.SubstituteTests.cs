using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Shared.Core.UnitTests.Visitors
{
    /// <summary>
    /// Tests for AnonymizationVisitor substitution functionality.
    /// </summary>
    public class AnonymizationVisitorSubstituteTests : AnonymizationVisitorTestBase
    {
        [Fact]
        public void GivenAPatientWithName_WhenSubstituted_NameShouldBeReplaced()
        {
            // Arrange
            var patient = CreateTestPatient();
            const string replacementName = "Anonymous";
            var config = CreateConfigurationWithSubstitute("Patient.name.family", replacementName);

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.Equal(replacementName, anonymized.Name[0].Family);
        }

        [Fact]
        public void GivenAPatient_WhenIdSubstituted_IdShouldBeReplaced()
        {
            // Arrange
            var patient = CreateTestPatient();
            const string replacementId = "anonymous-id";
            var config = CreateConfigurationWithSubstitute("Patient.id", replacementId);

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.Equal(replacementId, anonymized.Id);
        }

        [Fact]
        public void GivenAPatientWithAddress_WhenCitySubstituted_CityShouldBeReplaced()
        {
            // Arrange
            var patient = CreateTestPatient();
            const string replacementCity = "Unknown City";
            var config = CreateConfigurationWithSubstitute("Patient.address.city", replacementCity);

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.Equal(replacementCity, anonymized.Address[0].City);
        }

        [Fact]
        public void GivenAPatientWithTelecom_WhenValueSubstituted_ValueShouldBeReplaced()
        {
            // Arrange
            var patient = CreateTestPatient();
            const string replacementPhone = "000-0000";
            var config = CreateConfigurationWithSubstitute("Patient.telecom.value", replacementPhone);

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.Equal(replacementPhone, anonymized.Telecom[0].Value);
        }

        [Fact]
        public void GivenMultipleFields_WhenSubstituted_AllFieldsShouldBeReplaced()
        {
            // Arrange
            var patient = CreateTestPatient();
            const string familyReplacement = "Doe";
            const string cityReplacement = "Generic City";
            var config = CreateDefaultConfiguration();
            config.FhirPathRules.Add(CreateRule("Patient.name.family", "substitute",
                new Dictionary<string, object> { ["replaceWith"] = familyReplacement }));
            config.FhirPathRules.Add(CreateRule("Patient.address.city", "substitute",
                new Dictionary<string, object> { ["replaceWith"] = cityReplacement }));

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.Equal(familyReplacement, anonymized.Name[0].Family);
            Assert.Equal(cityReplacement, anonymized.Address[0].City);
        }

        [Fact]
        public void GivenAPatientWithMultipleGivenNames_WhenGivenNamesSubstituted_AllShouldBeReplaced()
        {
            // Arrange
            var patient = CreateTestPatient();
            const string replacement = "John";
            var config = CreateConfigurationWithSubstitute("Patient.name.given", replacement);

            // Act
            var anonymized = AnonymizeResource(patient, config);

            // Assert
            Assert.All(anonymized.Name[0].Given, given => Assert.Equal(replacement, given));
        }

        [Fact]
        public void GivenAnOrganizationWithName_WhenNameSubstituted_NameShouldBeReplaced()
        {
            // Arrange
            var organization = CreateTestOrganization();
            const string replacementName = "Anonymous Organization";
            var config = CreateConfigurationWithSubstitute("Organization.name", replacementName);

            // Act
            var anonymized = AnonymizeResource(organization, config);

            // Assert
            Assert.Equal(replacementName, anonymized.Name);
        }
    }
}
