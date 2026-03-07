using System;
using System.Linq;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests
{
    public class GDPRConfigurationTests
    {
        private const string GdprConfigPath = "../../../../Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool/configuration-gdpr-article89.json";

        public GDPRConfigurationTests()
        {
            FhirPathCompiler.DefaultSymbolTable.AddExtensionSymbols();
        }

        [Fact]
        public void GivenGDPRConfiguration_WhenLoaded_ConfigurationShouldLoadSuccessfully()
        {
            // Act
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(GdprConfigPath);

            // Assert
            Assert.NotNull(configurationManager);
            Assert.NotNull(configurationManager.Configuration);
            Assert.NotNull(configurationManager.FhirPathRules);
            Assert.True(configurationManager.FhirPathRules.Any());
        }

        [Fact]
        public void GivenGDPRConfiguration_WhenLoaded_ShouldHaveCorrectFhirVersion()
        {
            // Act
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(GdprConfigPath);

            // Assert
            Assert.Equal("R4", configurationManager.Configuration.FhirVersion);
        }

        [Fact]
        public void GivenGDPRConfiguration_WhenLoaded_ShouldHaveRequiredParameters()
        {
            // Act
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(GdprConfigPath);
            var parameters = configurationManager.GetParameterConfiguration();

            // Assert
            Assert.NotNull(parameters);
            Assert.NotNull(parameters.DateShiftKey);
            Assert.NotNull(parameters.CryptoHashKey);
            Assert.False(parameters.EnablePartialAgesForRedact);
            Assert.False(parameters.EnablePartialDatesForRedact);
            Assert.False(parameters.EnablePartialZipCodesForRedact);
        }

        [Fact]
        public void GivenGDPRConfiguration_WhenLoaded_ShouldHaveIdentifierPseudonymizationRules()
        {
            // Act
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(GdprConfigPath);
            var rules = configurationManager.FhirPathRules;

            // Assert - Verify identifier pseudonymization rules exist
            Assert.Contains(rules, r => r.Path.Contains("identifier") && r.Method == "cryptoHash");
        }

        [Fact]
        public void GivenGDPRConfiguration_WhenLoaded_ShouldHaveNameRedactionRules()
        {
            // Act
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(GdprConfigPath);
            var rules = configurationManager.FhirPathRules;

            // Assert - Verify name redaction rules exist
            Assert.Contains(rules, r => r.Path.Contains("HumanName") && r.Method == "redact");
        }

        [Fact]
        public void GivenGDPRConfiguration_WhenLoaded_ShouldHaveDateShiftRules()
        {
            // Act
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(GdprConfigPath);
            var rules = configurationManager.FhirPathRules;

            // Assert - Verify date shift rules exist
            // Note: method name uses lowercase convention "dateshift" in config files
            Assert.Contains(rules, r => r.Path.Contains("date") && r.Method.Equals("dateshift", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void GivenGDPRConfiguration_WhenLoaded_ShouldHaveContactRedactionRules()
        {
            // Act
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(GdprConfigPath);
            var rules = configurationManager.FhirPathRules;

            // Assert - Verify telecom/contact redaction rules exist
            Assert.Contains(rules, r => r.Path.Contains("telecom") && r.Method == "redact");
        }

        [Fact]
        public void GivenGDPRConfiguration_WhenLoaded_ShouldHaveAddressRedactionRules()
        {
            // Act
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(GdprConfigPath);
            var rules = configurationManager.FhirPathRules;

            // Assert - Verify address redaction rules exist
            Assert.Contains(rules, r => r.Path.Contains("address") && r.Method == "redact");
        }

        [Fact]
        public void GivenGDPRConfiguration_WhenLoaded_ShouldHaveGeneticDataRedactionRules()
        {
            // Act
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(GdprConfigPath);
            var rules = configurationManager.FhirPathRules;

            // Assert - Verify genetic/hereditary data redaction rules exist
            // FamilyMemberHistory covers hereditary and genetic-related family data (GDPR Art. 9)
            Assert.Contains(rules, r => r.ResourceType == "FamilyMemberHistory");
        }

        [Fact]
        public void GivenGDPRConfiguration_WhenLoaded_ShouldHaveBiometricDataRedactionRules()
        {
            // Act
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(GdprConfigPath);
            var rules = configurationManager.FhirPathRules;

            // Assert - Verify biometric-related redaction rules exist
            // base64Binary covers binary attachment data including biometric images (GDPR Art. 9)
            Assert.Contains(rules, r => r.Path.Contains("base64Binary") && r.Method == "redact");
        }

        [Fact]
        public void GivenGDPRConfiguration_WhenLoaded_ShouldProcessErrorsCorrectly()
        {
            // Act
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(GdprConfigPath);

            // Assert
            Assert.Equal(ProcessingErrorsOption.Raise, configurationManager.Configuration.processingErrors);
        }

        [Fact]
        public void GivenGDPRConfiguration_WhenLoaded_ShouldHaveDeviceIdentifierPseudonymization()
        {
            // Act
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(GdprConfigPath);
            var rules = configurationManager.FhirPathRules;

            // Assert - Verify device identifier pseudonymization
            // R4 config uses Device.distinctIdentifier (capital I) and Device.udiCarrier for device IDs
            Assert.Contains(rules, r => r.ResourceType == "Device" && r.Method == "cryptoHash");
        }

        [Fact]
        public void GivenGDPRConfiguration_WhenLoaded_ShouldHaveLocationDetailRedaction()
        {
            // Act
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(GdprConfigPath);
            var rules = configurationManager.FhirPathRules;

            // Assert - Verify location position redaction
            Assert.Contains(rules, r => r.ResourceType == "Location" && r.Path.Contains("position") && r.Method == "redact");
        }

        [Fact]
        public void GivenGDPRConfiguration_WhenLoaded_RulesShouldBeValid()
        {
            // Act & Assert - This will throw if rules are invalid
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(GdprConfigPath);
            
            // Verify all rules can be retrieved without exceptions
            foreach (var rule in configurationManager.FhirPathRules)
            {
                Assert.NotNull(rule.Path);
                Assert.NotNull(rule.Method);
            }
        }
    }
}
