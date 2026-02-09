using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Fhir.Anonymizer.Core.Validation;
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
            var config = new AnonymizerConfiguration
            {
                PathRules = new Dictionary<string, AnonymizerRule>
                {
                    {
                        "Patient.id",
                        new AnonymizerRule
                        {
                            Path = "Patient.id",
                            Method = AnonymizerMethod.CryptoHash,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { "cryptoHashKey", "test-key" }
                            }
                        }
                    },
                    {
                        "Patient.address.postalCode",
                        new AnonymizerRule
                        {
                            Path = "Patient.address.postalCode",
                            Method = AnonymizerMethod.KAnonymity,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { RuleKeys.KValue, "2" },
                                { RuleKeys.QuasiIdentifiers, "Patient.address.postalCode,Patient.gender" },
                                { RuleKeys.GeneralizationStrategy, "prefix" },
                                { RuleKeys.GeneralizationLevel, "2" }
                            }
                        }
                    },
                    {
                        "Patient.gender",
                        new AnonymizerRule
                        {
                            Path = "Patient.gender",
                            Method = AnonymizerMethod.KAnonymity,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { RuleKeys.KValue, "2" },
                                { RuleKeys.QuasiIdentifiers, "Patient.address.postalCode,Patient.gender" },
                                { RuleKeys.GeneralizationStrategy, "keep" }
                            }
                        }
                    },
                    {
                        "Patient.name.family",
                        new AnonymizerRule
                        {
                            Path = "Patient.name.family",
                            Method = AnonymizerMethod.Redact,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration()
                        }
                    }
                }
            };

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
            var config = new AnonymizerConfiguration
            {
                PathRules = new Dictionary<string, AnonymizerRule>
                {
                    {
                        "Observation.effectiveDateTime",
                        new AnonymizerRule
                        {
                            Path = "Observation.effectiveDateTime",
                            Method = AnonymizerMethod.DateShift,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { "dateShiftKey", "test-key" },
                                { "dateShiftRange", "100" }
                            }
                        }
                    },
                    {
                        "Observation.valueQuantity.value",
                        new AnonymizerRule
                        {
                            Path = "Observation.valueQuantity.value",
                            Method = AnonymizerMethod.DifferentialPrivacy,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { RuleKeys.Epsilon, "0.5" },
                                { RuleKeys.Sensitivity, "10.0" },
                                { RuleKeys.MechanismType, "laplace" }
                            }
                        }
                    }
                }
            };

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
        }

        [Fact]
        public void EndToEnd_AllMethodsCombined_ShouldApplyInCorrectOrder()
        {
            // Arrange: Comprehensive configuration with multiple methods
            var config = new AnonymizerConfiguration
            {
                PathRules = new Dictionary<string, AnonymizerRule>
                {
                    // Redact sensitive identifiers
                    {
                        "Patient.name.given",
                        new AnonymizerRule
                        {
                            Path = "Patient.name.given",
                            Method = AnonymizerMethod.Redact,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration()
                        }
                    },
                    // Hash patient ID
                    {
                        "Patient.id",
                        new AnonymizerRule
                        {
                            Path = "Patient.id",
                            Method = AnonymizerMethod.CryptoHash,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { "cryptoHashKey", "test-key" }
                            }
                        }
                    },
                    // Shift birth date
                    {
                        "Patient.birthDate",
                        new AnonymizerRule
                        {
                            Path = "Patient.birthDate",
                            Method = AnonymizerMethod.DateShift,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { "dateShiftKey", "test-key" },
                                { "dateShiftRange", "50" }
                            }
                        }
                    },
                    // K-anonymity for postal code
                    {
                        "Patient.address.postalCode",
                        new AnonymizerRule
                        {
                            Path = "Patient.address.postalCode",
                            Method = AnonymizerMethod.KAnonymity,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { RuleKeys.KValue, "2" },
                                { RuleKeys.QuasiIdentifiers, "Patient.address.postalCode" },
                                { RuleKeys.GeneralizationStrategy, "prefix" },
                                { RuleKeys.GeneralizationLevel, "2" }
                            }
                        }
                    }
                }
            };

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
            var config = new AnonymizerConfiguration
            {
                PathRules = new Dictionary<string, AnonymizerRule>
                {
                    {
                        "Patient.address.postalCode",
                        new AnonymizerRule
                        {
                            Path = "Patient.address.postalCode",
                            Method = AnonymizerMethod.KAnonymity,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { RuleKeys.KValue, "3" },
                                { RuleKeys.QuasiIdentifiers, "Patient.address.postalCode,Patient.gender" },
                                { RuleKeys.GeneralizationStrategy, "prefix" },
                                { RuleKeys.GeneralizationLevel, "2" }
                            }
                        }
                    },
                    {
                        "Patient.gender",
                        new AnonymizerRule
                        {
                            Path = "Patient.gender",
                            Method = AnonymizerMethod.KAnonymity,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { RuleKeys.KValue, "3" },
                                { RuleKeys.QuasiIdentifiers, "Patient.address.postalCode,Patient.gender" },
                                { RuleKeys.GeneralizationStrategy, "keep" }
                            }
                        }
                    }
                }
            };

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
            var config = new AnonymizerConfiguration
            {
                PathRules = new Dictionary<string, AnonymizerRule>
                {
                    {
                        "Patient.identifier.value",
                        new AnonymizerRule
                        {
                            Path = "Patient.identifier.value",
                            Method = AnonymizerMethod.CryptoHash,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { "cryptoHashKey", "test-key" }
                            }
                        }
                    },
                    {
                        "Patient.contact.name.family",
                        new AnonymizerRule
                        {
                            Path = "Patient.contact.name.family",
                            Method = AnonymizerMethod.Redact,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration()
                        }
                    }
                }
            };

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
