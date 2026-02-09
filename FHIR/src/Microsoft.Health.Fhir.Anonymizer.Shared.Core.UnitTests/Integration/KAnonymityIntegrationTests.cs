using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Fhir.Anonymizer.Core.Validation;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Integration
{
    public class KAnonymityIntegrationTests
    {
        [Fact]
        public void EndToEnd_KAnonymityOnPatientResources_ShouldAnonymizeSuccessfully()
        {
            // Arrange: Create configuration with k-anonymity
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
                                { RuleKeys.QuasiIdentifiers, "Patient.address.postalCode,Patient.gender,Patient.birthDate" },
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
                                { RuleKeys.QuasiIdentifiers, "Patient.address.postalCode,Patient.gender,Patient.birthDate" },
                                { RuleKeys.GeneralizationStrategy, "keep" }
                            }
                        }
                    },
                    {
                        "Patient.birthDate",
                        new AnonymizerRule
                        {
                            Path = "Patient.birthDate",
                            Method = AnonymizerMethod.KAnonymity,
                            Cases = new List<AnonymizerRule>(),
                            Parameters = new ParameterConfiguration
                            {
                                { RuleKeys.KValue, "3" },
                                { RuleKeys.QuasiIdentifiers, "Patient.address.postalCode,Patient.gender,Patient.birthDate" },
                                { RuleKeys.GeneralizationStrategy, "year" }
                            }
                        }
                    }
                }
            };

            var engine = new AnonymizerEngine(config);

            // Create test patient resources
            var patients = new List<string>
            {
                CreatePatientJson("98101", "male", "1990-05-15"),
                CreatePatientJson("98102", "male", "1990-07-20"),
                CreatePatientJson("98103", "male", "1990-03-10"),
                CreatePatientJson("98201", "female", "1985-08-25"),
                CreatePatientJson("98202", "female", "1985-11-30"),
                CreatePatientJson("98203", "female", "1985-01-05")
            };

            // Act: Anonymize all patients
            var anonymizedPatients = patients.Select(p => engine.AnonymizeJson(p)).ToList();

            // Assert: Verify k-anonymity is achieved
            var validator = new KAnonymityValidator();
            var quasiIdentifiers = new[] { "Patient.address.postalCode", "Patient.gender", "Patient.birthDate" };
            var result = validator.ValidateKAnonymity(anonymizedPatients, quasiIdentifiers, 3);

            Assert.True(result.IsKAnonymized, "Dataset should satisfy k-anonymity");
            Assert.Equal(3, result.KValue);
            Assert.Empty(result.Violations);
            Assert.True(result.MinimumClassSize >= 3, "Minimum class size should be at least k");
        }

        [Fact]
        public void EndToEnd_KAnonymityWithSuppression_ShouldSuppressOutliers()
        {
            // Arrange: Create configuration with k-anonymity and high k-value
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
                                { RuleKeys.KValue, "5" },
                                { RuleKeys.QuasiIdentifiers, "Patient.address.postalCode,Patient.gender" },
                                { RuleKeys.GeneralizationStrategy, "suppress" },
                                { RuleKeys.SuppressionThreshold, "0.5" }
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
                                { RuleKeys.KValue, "5" },
                                { RuleKeys.QuasiIdentifiers, "Patient.address.postalCode,Patient.gender" },
                                { RuleKeys.GeneralizationStrategy, "suppress" }
                            }
                        }
                    }
                }
            };

            var engine = new AnonymizerEngine(config);

            // Create patients with some outliers
            var patients = new List<string>
            {
                CreatePatientJson("98101", "male", "1990-05-15"),
                CreatePatientJson("98101", "male", "1990-07-20"),
                CreatePatientJson("98101", "male", "1990-03-10"),
                CreatePatientJson("98101", "male", "1985-08-25"),
                CreatePatientJson("98101", "male", "1985-11-30"),
                CreatePatientJson("99999", "other", "1980-01-05") // Outlier
            };

            // Act: Anonymize all patients
            var anonymizedPatients = patients.Select(p => engine.AnonymizeJson(p)).ToList();

            // Assert: Verify suppression occurred
            var outlierPatient = JObject.Parse(anonymizedPatients[5]);
            var postalCode = outlierPatient.SelectToken("address[0].postalCode");
            var gender = outlierPatient.SelectToken("gender");

            // Outlier should be suppressed (removed or masked)
            Assert.True(postalCode == null || string.IsNullOrEmpty(postalCode.ToString()),
                "Outlier postal code should be suppressed");
        }

        [Fact]
        public void EndToEnd_KAnonymityWithGeneralization_ShouldGeneralizeData()
        {
            // Arrange: Create configuration with range-based generalization
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
                                { RuleKeys.KValue, "2" },
                                { RuleKeys.QuasiIdentifiers, "Patient.address.postalCode" },
                                { RuleKeys.GeneralizationStrategy, "prefix" },
                                { RuleKeys.GeneralizationLevel, "3" }
                            }
                        }
                    }
                }
            };

            var engine = new AnonymizerEngine(config);

            // Create patients with similar postal codes
            var patients = new List<string>
            {
                CreatePatientJson("98101", "male", "1990-05-15"),
                CreatePatientJson("98102", "female", "1985-08-25")
            };

            // Act: Anonymize patients
            var anonymizedPatients = patients.Select(p => engine.AnonymizeJson(p)).ToList();

            // Assert: Verify generalization occurred
            foreach (var patient in anonymizedPatients)
            {
                var patientObj = JObject.Parse(patient);
                var postalCode = patientObj.SelectToken("address[0].postalCode")?.ToString();

                Assert.NotNull(postalCode);
                // Should be generalized to prefix (e.g., "981**")
                Assert.True(postalCode.Length == 5, "Postal code should maintain 5 characters");
                Assert.StartsWith("981", postalCode);
            }
        }

        [Fact]
        public void EndToEnd_KAnonymityValidation_ShouldReportStatistics()
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
                    }
                }
            };

            var engine = new AnonymizerEngine(config);
            var validator = new KAnonymityValidator();
            var riskAssessor = new ReidentificationRiskAssessor();

            var patients = new List<string>
            {
                CreatePatientJson("98101", "male", "1990-05-15"),
                CreatePatientJson("98102", "male", "1990-07-20"),
                CreatePatientJson("98201", "female", "1985-08-25"),
                CreatePatientJson("98202", "female", "1985-11-30")
            };

            // Act
            var anonymizedPatients = patients.Select(p => engine.AnonymizeJson(p)).ToList();
            var quasiIdentifiers = new[] { "Patient.address.postalCode", "Patient.gender" };
            var validationResult = validator.ValidateKAnonymity(anonymizedPatients, quasiIdentifiers, 2);
            var riskResult = riskAssessor.AssessReidentificationRisk(anonymizedPatients, quasiIdentifiers);

            // Assert
            Assert.True(validationResult.IsKAnonymized);
            Assert.Equal(2, validationResult.KValue);
            Assert.True(validationResult.TotalRecords == 4);
            Assert.True(validationResult.TotalEquivalenceClasses > 0);
            Assert.Equal(0.0, validationResult.SuppressionRate);

            Assert.True(riskResult.ProsecutorRisk <= 1.0);
            Assert.True(riskResult.JournalistRisk <= 1.0);
            Assert.InRange(riskResult.UniquenessRatio, 0.0, 1.0);
            Assert.Contains(riskResult.RiskLevel, new[] { "Low", "Medium", "High" });
        }

        [Fact]
        public void EndToEnd_KAnonymityWithMissingData_ShouldHandleGracefully()
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
                                { RuleKeys.KValue, "2" },
                                { RuleKeys.QuasiIdentifiers, "Patient.address.postalCode,Patient.gender" },
                                { RuleKeys.GeneralizationStrategy, "prefix" },
                                { RuleKeys.GeneralizationLevel, "2" }
                            }
                        }
                    }
                }
            };

            var engine = new AnonymizerEngine(config);

            // Create patients with missing data
            var patientsJson = new List<string>
            {
                @"{""resourceType"": ""Patient"", ""gender"": ""male""}", // Missing address
                CreatePatientJson("98101", "male", "1990-05-15"),
                CreatePatientJson("98102", "male", "1990-07-20")
            };

            // Act
            var anonymizedPatients = patientsJson.Select(p => engine.AnonymizeJson(p)).ToList();

            // Assert: Should not throw, and should handle missing data gracefully
            Assert.Equal(3, anonymizedPatients.Count);
            foreach (var patient in anonymizedPatients)
            {
                var patientObj = JObject.Parse(patient);
                Assert.Equal("Patient", patientObj["resourceType"]?.ToString());
            }
        }

        [Fact]
        public void EndToEnd_ConfigurationLoading_ShouldValidateKAnonymitySettings()
        {
            // Arrange: Create configuration with invalid k-value
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
                                { RuleKeys.KValue, "1" }, // Invalid: k must be >= 2
                                { RuleKeys.QuasiIdentifiers, "Patient.address.postalCode" },
                                { RuleKeys.GeneralizationStrategy, "prefix" }
                            }
                        }
                    }
                }
            };

            // Act & Assert: Should throw during engine creation
            Assert.Throws<ArgumentException>(() => new AnonymizerEngine(config));
        }

        [Fact]
        public void EndToEnd_ConfigurationLoading_ShouldValidateQuasiIdentifiers()
        {
            // Arrange: Create configuration with missing quasi-identifiers
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
                                { RuleKeys.KValue, "2" },
                                // Missing quasi-identifiers
                                { RuleKeys.GeneralizationStrategy, "prefix" }
                            }
                        }
                    }
                }
            };

            // Act & Assert: Should throw during engine creation
            Assert.Throws<ArgumentException>(() => new AnonymizerEngine(config));
        }

        private string CreatePatientJson(string postalCode, string gender, string birthDate)
        {
            return $@"{{
                ""resourceType"": ""Patient"",
                ""id"": ""{Guid.NewGuid()}"",
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
