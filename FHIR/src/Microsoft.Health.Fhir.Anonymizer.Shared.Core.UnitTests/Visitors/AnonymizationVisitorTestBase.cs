using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Microsoft.Health.Fhir.Anonymizer.Core.Visitors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Shared.Core.UnitTests.Visitors
{
    /// <summary>
    /// Base class for AnonymizationVisitor tests providing common test fixtures and helper methods.
    /// </summary>
    public abstract class AnonymizationVisitorTestBase
    {
        protected readonly FhirJsonParser Parser = new FhirJsonParser();
        protected readonly FhirJsonSerializer Serializer = new FhirJsonSerializer();
        protected const string TestDateShiftKey = "test-date-shift-key";
        protected const string TestEncryptKey = "test-encrypt-key-123";

        /// <summary>
        /// Creates a default anonymizer configuration.
        /// </summary>
        protected AnonymizerConfiguration CreateDefaultConfiguration()
        {
            return new AnonymizerConfiguration
            {
                FhirVersion = "R4",
                FhirPathRules = new List<AnonymizerRule>()
            };
        }

        /// <summary>
        /// Creates an anonymizer configuration with specified rules.
        /// </summary>
        protected AnonymizerConfiguration CreateAnonymizerConfiguration(
            string fhirPathRule,
            string method,
            Dictionary<string, object> parameters = null)
        {
            var config = CreateDefaultConfiguration();
            config.FhirPathRules.Add(new AnonymizerRule(
                path: fhirPathRule,
                method: method,
                description: $"Test rule for {method}",
                ruleSettings: parameters ?? new Dictionary<string, object>()));
            return config;
        }

        /// <summary>
        /// Creates a simple anonymizer rule.
        /// </summary>
        protected AnonymizerRule CreateRule(string path, string method, Dictionary<string, object> parameters = null)
        {
            return new AnonymizerRule(
                path: path,
                method: method,
                description: $"Test rule for {method}",
                ruleSettings: parameters ?? new Dictionary<string, object>());
        }

        /// <summary>
        /// Creates a configuration with redaction for a specific path.
        /// </summary>
        protected AnonymizerConfiguration CreateConfigurationWithRedaction(string fhirPath)
        {
            return CreateAnonymizerConfiguration(fhirPath, "redact");
        }

        /// <summary>
        /// Creates a configuration with date shifting.
        /// </summary>
        protected AnonymizerConfiguration CreateConfigurationWithDateShift(string fhirPath, string dateShiftKey = null)
        {
            var parameters = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(dateShiftKey))
            {
                parameters["dateShiftKey"] = dateShiftKey;
            }
            return CreateAnonymizerConfiguration(fhirPath, "dateShift", parameters);
        }

        /// <summary>
        /// Creates a configuration with crypto hash.
        /// </summary>
        protected AnonymizerConfiguration CreateConfigurationWithCryptoHash(string fhirPath)
        {
            return CreateAnonymizerConfiguration(fhirPath, "cryptoHash");
        }

        /// <summary>
        /// Creates a configuration with encryption.
        /// </summary>
        protected AnonymizerConfiguration CreateConfigurationWithEncrypt(string fhirPath, string encryptKey)
        {
            var parameters = new Dictionary<string, object>
            {
                ["encryptKey"] = encryptKey
            };
            return CreateAnonymizerConfiguration(fhirPath, "encrypt", parameters);
        }

        /// <summary>
        /// Creates a configuration with substitution.
        /// </summary>
        protected AnonymizerConfiguration CreateConfigurationWithSubstitute(string fhirPath, string replaceWith)
        {
            var parameters = new Dictionary<string, object>
            {
                ["replaceWith"] = replaceWith
            };
            return CreateAnonymizerConfiguration(fhirPath, "substitute", parameters);
        }

        /// <summary>
        /// Creates an AnonymizationVisitor with the given configuration.
        /// </summary>
        protected AnonymizationVisitor CreateVisitor(AnonymizerConfiguration configuration)
        {
            var engine = AnonymizerEngine.CreateWithConfiguration(configuration);
            return new AnonymizationVisitor(engine, configuration);
        }

        /// <summary>
        /// Anonymizes a FHIR resource using the provided configuration.
        /// </summary>
        protected T AnonymizeResource<T>(T resource, AnonymizerConfiguration configuration) where T : Resource
        {
            var visitor = CreateVisitor(configuration);
            return AnonymizeResource(resource, visitor);
        }

        /// <summary>
        /// Anonymizes a FHIR resource using the provided visitor.
        /// </summary>
        protected T AnonymizeResource<T>(T resource, AnonymizationVisitor visitor) where T : Resource
        {
            var processor = new AnonymizationFhirProcessor(visitor);
            return processor.AnonymizeResource(resource) as T;
        }

        /// <summary>
        /// Creates a test Patient resource with standard test data.
        /// </summary>
        protected Patient CreateTestPatient()
        {
            return new Patient
            {
                Id = "test-patient-123",
                Active = true,
                BirthDate = "1980-01-01",
                Gender = AdministrativeGender.Male,
                Name = new List<HumanName>
                {
                    new HumanName
                    {
                        Family = "Smith",
                        Given = new[] { "John", "Jacob" }
                    }
                },
                Address = new List<Address>
                {
                    new Address
                    {
                        Line = new[] { "123 Main St" },
                        City = "Springfield",
                        State = "IL",
                        PostalCode = "62701",
                        Country = "USA"
                    }
                },
                Telecom = new List<ContactPoint>
                {
                    new ContactPoint
                    {
                        System = ContactPoint.ContactPointSystem.Phone,
                        Value = "555-1234"
                    }
                }
            };
        }

        /// <summary>
        /// Creates a test Observation resource.
        /// </summary>
        protected Observation CreateTestObservation()
        {
            return new Observation
            {
                Id = "test-observation-456",
                Status = ObservationStatus.Final,
                Code = new CodeableConcept
                {
                    Text = "Blood Pressure"
                },
                Effective = new FhirDateTime("2023-01-15T10:30:00Z"),
                Issued = new DateTimeOffset(2023, 1, 15, 10, 30, 0, TimeSpan.Zero),
                Subject = new ResourceReference("Patient/test-patient-123")
            };
        }

        /// <summary>
        /// Creates a test Organization resource.
        /// </summary>
        protected Organization CreateTestOrganization()
        {
            return new Organization
            {
                Id = "test-org-789",
                Active = true,
                Name = "Test Hospital",
                Address = new List<Address>
                {
                    new Address
                    {
                        Line = new[] { "456 Hospital Rd" },
                        City = "Springfield",
                        State = "IL",
                        PostalCode = "62702"
                    }
                }
            };
        }

        /// <summary>
        /// Asserts that a string value has been redacted (is null or empty).
        /// </summary>
        protected void AssertRedacted(string value)
        {
            Assert.True(string.IsNullOrEmpty(value), "Value should be redacted");
        }

        /// <summary>
        /// Asserts that a value is not null and not empty.
        /// </summary>
        protected void AssertNotRedacted(string value)
        {
            Assert.False(string.IsNullOrEmpty(value), "Value should not be redacted");
        }
    }
}
