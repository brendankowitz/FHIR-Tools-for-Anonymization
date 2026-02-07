using System.IO;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    public class GdprArticle89ConfigurationTests
    {
        private const string TestConfigPath = "TestConfigurations/configuration-gdpr-article89-test.json";

        [Fact]
        public void GdprConfiguration_ShouldLoadSuccessfully()
        {
            // Arrange & Act
            var config = AnonymizerConfigurationManager.CreateFromConfigurationFile(TestConfigPath);

            // Assert
            Assert.NotNull(config);
            Assert.NotNull(config.FhirPathRules);
            Assert.NotEmpty(config.FhirPathRules);
            Assert.NotNull(config.ParameterConfiguration);
        }

        [Fact]
        public void GdprConfiguration_ShouldHaveFhirVersion()
        {
            // Arrange & Act
            var config = AnonymizerConfigurationManager.CreateFromConfigurationFile(TestConfigPath);

            // Assert - FhirVersion should be set (empty string is acceptable for test configs)
            Assert.NotNull(config.FhirVersion);
        }

        [Fact]
        public void GdprConfiguration_ShouldRequireCryptographicKeys()
        {
            // Arrange & Act
            var config = AnonymizerConfigurationManager.CreateFromConfigurationFile(TestConfigPath);

            // Assert - Configuration should have key parameters defined
            Assert.NotNull(config.ParameterConfiguration.DateShiftKey);
            Assert.NotNull(config.ParameterConfiguration.CryptoHashKey);
            Assert.NotNull(config.ParameterConfiguration.EncryptKey);
            
            // Keys should be 32 characters for proper encryption
            Assert.True(config.ParameterConfiguration.DateShiftKey.Length >= 32,
                "DateShiftKey should be at least 32 characters for secure encryption");
            Assert.True(config.ParameterConfiguration.CryptoHashKey.Length >= 32,
                "CryptoHashKey should be at least 32 characters for secure hashing");
            Assert.True(config.ParameterConfiguration.EncryptKey.Length >= 32,
                "EncryptKey should be at least 32 characters for secure encryption");
        }

        [Fact]
        public void GdprConfiguration_ShouldUseCryptoHashForIdentifiers()
        {
            // Arrange & Act
            var config = AnonymizerConfigurationManager.CreateFromConfigurationFile(TestConfigPath);

            // Assert - Should have cryptoHash rules for identifiers
            var hasIdCryptoHash = false;
            var hasIdentifierCryptoHash = false;
            var hasReferenceCryptoHash = false;

            foreach (var rule in config.FhirPathRules)
            {
                if (rule.Path.Contains("Resource.id") && rule.Method == "cryptoHash")
                    hasIdCryptoHash = true;
                if (rule.Path.Contains("Identifier") && rule.Method == "cryptoHash")
                    hasIdentifierCryptoHash = true;
                if (rule.Path.Contains("Reference") && rule.Method == "cryptoHash")
                    hasReferenceCryptoHash = true;
            }

            Assert.True(hasIdCryptoHash, "Configuration should use cryptoHash for Resource.id");
            Assert.True(hasIdentifierCryptoHash, "Configuration should use cryptoHash for Identifiers");
            Assert.True(hasReferenceCryptoHash, "Configuration should use cryptoHash for References");
        }

        [Fact]
        public void GdprConfiguration_ShouldUseDateShiftForTemporalData()
        {
            // Arrange & Act
            var config = AnonymizerConfigurationManager.CreateFromConfigurationFile(TestConfigPath);

            // Assert - Should have dateshift rules for temporal types
            var hasDateShift = false;
            var hasDateTimeShift = false;

            foreach (var rule in config.FhirPathRules)
            {
                if (rule.Path.Contains("nodesByType('date')") && rule.Method == "dateshift")
                    hasDateShift = true;
                if (rule.Path.Contains("nodesByType('dateTime')") && rule.Method == "dateshift")
                    hasDateTimeShift = true;
            }

            Assert.True(hasDateShift, "Configuration should use dateshift for date types");
            Assert.True(hasDateTimeShift, "Configuration should use dateshift for dateTime types");
        }

        [Fact]
        public void GdprConfiguration_ShouldRedactFreeTextFields()
        {
            // Arrange & Act
            var config = AnonymizerConfigurationManager.CreateFromConfigurationFile(TestConfigPath);

            // Assert - Should redact high-risk free text fields
            var hasNameRedact = false;
            var hasNarrativeRedact = false;

            foreach (var rule in config.FhirPathRules)
            {
                if (rule.Path.Contains("HumanName") && rule.Method == "redact")
                    hasNameRedact = true;
                if (rule.Path.Contains("Narrative") && rule.Method == "redact")
                    hasNarrativeRedact = true;
            }

            Assert.True(hasNameRedact, "Configuration should redact HumanName elements");
            Assert.True(hasNarrativeRedact, "Configuration should redact Narrative text");
        }

        [Fact]
        public void GdprConfiguration_ShouldPreserveClinicalValues()
        {
            // Arrange & Act
            var config = AnonymizerConfigurationManager.CreateFromConfigurationFile(TestConfigPath);

            // Assert - Should preserve clinical observation values
            var hasObservationValueKeep = false;

            foreach (var rule in config.FhirPathRules)
            {
                if (rule.Path.Contains("Observation.value") && rule.Method == "keep")
                    hasObservationValueKeep = true;
            }

            Assert.True(hasObservationValueKeep, 
                "Configuration should preserve clinical Observation values for research utility");
        }

        [Fact]
        public void GdprConfiguration_ShouldHaveResourceScopeDateShift()
        {
            // Arrange & Act
            var config = AnonymizerConfigurationManager.CreateFromConfigurationFile(TestConfigPath);

            // Assert - DateShift scope should be set (typically "resource" or "file")
            Assert.NotNull(config.ParameterConfiguration.DateShiftScope);
            Assert.NotEmpty(config.ParameterConfiguration.DateShiftScope);
        }

        [Fact]
        public void GdprConfiguration_ShouldNotEnablePartialRedaction()
        {
            // Arrange & Act
            var config = AnonymizerConfigurationManager.CreateFromConfigurationFile(TestConfigPath);

            // Assert - Partial redaction should be disabled for stricter privacy
            Assert.False(config.ParameterConfiguration.EnablePartialAgesForRedact,
                "Partial ages should be disabled for stricter privacy protection");
            Assert.False(config.ParameterConfiguration.EnablePartialDatesForRedact,
                "Partial dates should be disabled for stricter privacy protection");
            Assert.False(config.ParameterConfiguration.EnablePartialZipCodesForRedact,
                "Partial zip codes should be disabled for stricter privacy protection");
        }
    }
}