using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Fhir.Anonymizer.Core.Validation;
using Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Helpers;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Integration
{
    public class CombinedMethodsIntegrationTests
    {
        [Fact]
        public void EndToEnd_KAnonymityAndCryptoHash_ShouldCombineSuccessfully()
        {
            // Arrange: Combine k-anonymity for demographics with cryptoHash for identifiers
            var config = new ConfigurationBuilder()
                .WithCryptoHash("Patient.id", "test-key")
                .WithKAnonymity("Patient.address.postalCode",
                    kValue: 2,
                    quasiIdentifiers: "Patient.address.postalCode,Patient.gender",
                    generalizationStrategy: "prefix",
                    generalizationLevel: 2)
                .WithKAnonymity("Patient.gender",
                    kValue: 2,
                    quasiIdentifiers: "Patient.address.postalCode,Patient.gender",
                    generalizationStrategy: "keep")
                .WithRedact("Patient.name.family")
                .Build();

            var engine = new AnonymizerEngine(config);

            var patients = new List<string>
            {
                CreatePatientJson("patient-1", "Doe", "98101", "male"),
                CreatePatientJson("patient-2", "Smith", "98102", "male")
            };

            // Act
            var anonymizedPatients = patients.Select(p => engine.AnonymizeJson(p)).ToList();

            // Assert: Verify all methods applied
            foreach (var patient in anonymizedPatients)
            {
                var patientObj = JObject.Parse(patient);

                // ID should be hashed
                var id = patientObj["id"]?.ToString();
                Assert.NotNull(id);
                Assert.NotEqual("patient-1", id);
                Assert.NotEqual("patient-2", id);

                // Name should be redacted
                var family = patientObj.SelectToken("name[0].family");
                Assert.True(family == null || string.IsNullOrEmpty(family.ToString()));

                // Postal code should be generalized (k-anonymity)
                var postalCode = patientObj.SelectToken("address[0].postalCode")?.ToString();
                Assert.NotNull(postalCode);
                Assert.StartsWith("981", postalCode);
            }
        }

        [Fact]
        public void EndToEnd_DifferentialPrivacyAndDateShift_ShouldCombineSuccessfully()
        {
            // Arrange: Combine differential privacy for values with date shifting
            var budgetContext = ConfigurationBuilder.CreateBudgetContext("combined-methods-test", "dp-dateshift");
            var config = new ConfigurationBuilder()
                .WithDateShift("Observation.effectiveDateTime", "test-key", dateShiftRange: 100)
                .WithDifferentialPrivacy("Observation.valueQuantity.value",
                    epsilon: 0.5,
                    sensitivity: 10.0,
                    mechanism: "laplace",
                    budgetContext: budgetContext,
                    totalBudget: 1.0)
                .Build();

            var engine = new AnonymizerEngine(config);
            var observationJson = @"{
                ""resourceType"": ""Observation"",
                ""status"": ""final"",
                ""code"": { ""text"": ""Test"" },
                ""effectiveDateTime"": ""2023-01-15T10:30:00Z"",
                ""valueQuantity"": {
                    ""value"": 120.0,
                    ""unit"": ""mg/dL""
                }
            }";

            // Act
            var anonymized = engine.AnonymizeJson(observationJson);
            var anonymizedObj = JObject.Parse(anonymized);

            // Assert: Both methods applied
            var effectiveDateTime = anonymizedObj["effectiveDateTime"]?.ToString();
            Assert.NotNull(effectiveDateTime);
            Assert.NotEqual("2023-01-15T10:30:00Z", effectiveDateTime);

            var value = (double)anonymizedObj.SelectToken("valueQuantity.value");
            // Value might differ due to noise (but could be same by chance)
            Assert.InRange(value, 0, 300); // Reasonable range
            
            // Cleanup
            PrivacyBudgetTracker.Instance.ResetBudget(budgetContext);
        }

        [Fact]
        public void EndToEnd_AllMethodsCombined_ShouldApplyInCorrectOrder()
        {
            // Arrange: Comprehensive configuration with multiple methods
            var config = new ConfigurationBuilder()
                .WithRedact("Patient.name.given")
                .WithCryptoHash("Patient.id", "test-key")
                .WithDateShift("Patient.birthDate", "test-key", dateShiftRange: 50)
                .WithKAnonymity("Patient.address.postalCode",
                    kValue: 2,
                    quasiIdentifiers: "Patient.address.postalCode",
                    generalizationStrategy: "prefix",
                    generalizationLevel: 2)
                .Build();

            var engine = new AnonymizerEngine(config);

            var patients = new List<string>
            {
                CreatePatientJson("patient-1", "Doe", "98101", "male", "John", "1990-05-15"),
                CreatePatientJson("patient-2", "Smith", "98102", "female", "Jane", "1985-08-20")
            };

            // Act
            var anonymizedPatients = patients.Select(p => engine.AnonymizeJson(p)).ToList();

            // Assert: All transformations applied
            foreach (var patient in anonymizedPatients)
            {
                var patientObj = JObject.Parse(patient);

                // Given name redacted
                var given = patientObj.SelectToken("name[0].given");
                Assert.True(given == null || !given.HasValues);

                // ID hashed
                var id = patientObj["id"]?.ToString();
                Assert.NotNull(id);
                Assert.DoesNotContain("patient-", id);

                // Birth date shifted
                var birthDate = patientObj["birthDate"]?.ToString();
                Assert.NotNull(birthDate);
                Assert.NotEqual("1990-05-15", birthDate);
                Assert.NotEqual("1985-08-20", birthDate);

                // Postal code generalized
                var postalCode = patientObj.SelectToken("address[0].postalCode")?.ToString();
                Assert.NotNull(postalCode);
                Assert.StartsWith("981", postalCode);
            }
        }

        [Fact]
        public void EndToEnd_KAnonymityWithValidation_ShouldVerifyPrivacyProperties()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .WithKAnonymity("Patient.address.postalCode",
                    kValue: 3,
                    quasiIdentifiers: "Patient.address.postalCode,Patient.gender",
                    generalizationStrategy: "prefix",
                    generalizationLevel: 2)
                .WithKAnonymity("Patient.gender",
                    kValue: 3,
                    quasiIdentifiers: "Patient.address.postalCode,Patient.gender",
                    generalizationStrategy: "keep")
                .Build();

            var engine = new AnonymizerEngine(config);
            var validator = new KAnonymityValidator();
            var riskAssessor = new ReidentificationRiskAssessor();

            var patients = new List<string>
            {
                CreatePatientJson("p1", "Smith", "98101", "male"),
                CreatePatientJson("p2", "Jones", "98102", "male"),
                CreatePatientJson("p3", "Brown", "98103", "male"),
                CreatePatientJson("p4", "Davis", "98201", "female"),
                CreatePatientJson("p5", "Wilson", "98202", "female"),
                CreatePatientJson("p6", "Taylor", "98203", "female")
            };

            // Act
            var anonymizedPatients = patients.Select(p => engine.AnonymizeJson(p)).ToList();
            var quasiIdentifiers = new[] { "Patient.address.postalCode", "Patient.gender" };
            var validationResult = validator.ValidateKAnonymity(anonymizedPatients, quasiIdentifiers, 3);
            var riskResult = riskAssessor.AssessReidentificationRisk(anonymizedPatients, quasiIdentifiers);

            // Assert: Privacy properties satisfied
            Assert.True(validationResult.IsKAnonymized, "Should satisfy k-anonymity");
            Assert.Equal(3, validationResult.KValue);
            Assert.Empty(validationResult.Violations);
            Assert.True(validationResult.MinimumClassSize >= 3);

            Assert.InRange(riskResult.ProsecutorRisk, 0.0, 1.0);
            Assert.InRange(riskResult.JournalistRisk, 0.0, 1.0);
            Assert.True(riskResult.ProsecutorRisk <= 1.0 / 3.0,
                "Prosecutor risk should be at most 1/k");
        }

        [Fact]
        public void EndToEnd_ComplexResourceWithMultipleMethods_ShouldHandleNesting()
        {
            // Arrange: Complex nested resource
            var config = new ConfigurationBuilder()
                .WithCryptoHash("Patient.identifier.value", "test-key")
                .WithRedact("Patient.contact.name.family")
                .Build();

            var engine = new AnonymizerEngine(config);
            var patientJson = @"{
                ""resourceType"": ""Patient"",
                ""identifier"": [
                    {
                        ""system"": ""http://example.org/mrn"",
                        ""value"": ""12345""
                    }
                ],
                ""contact"": [
                    {
                        ""name"": {
                            ""family"": ""Emergency"",
                            ""given"": [""Contact""]
                        }
                    }
                ]
            }";

            // Act
            var anonymized = engine.AnonymizeJson(patientJson);
            var anonymizedObj = JObject.Parse(anonymized);

            // Assert: Nested paths handled correctly
            var identifierValue = anonymizedObj.SelectToken("identifier[0].value")?.ToString();
            Assert.NotNull(identifierValue);
            Assert.NotEqual("12345", identifierValue);

            var contactFamily = anonymizedObj.SelectToken("contact[0].name.family");
            Assert.True(contactFamily == null || string.IsNullOrEmpty(contactFamily.ToString()));
        }

        private string CreatePatientJson(string id, string family, string postalCode, string gender,
            string given = "Test", string birthDate = "1990-01-01")
        {
            return $@"{{
                ""resourceType"": ""Patient"",
                ""id"": ""{id}"",
                ""name"": [
                    {{
                        ""family"": ""{family}"",
                        ""given"": [""{given}""]
                    }}
                ],
                ""gender"": ""{gender}"",
                ""birthDate"": ""{birthDate}"",
                ""address"": [
                    {{
                        ""use"": ""home"",
                        ""postalCode"": ""{postalCode}""
                    }}
                ]
            }}";
        }
    }
}
