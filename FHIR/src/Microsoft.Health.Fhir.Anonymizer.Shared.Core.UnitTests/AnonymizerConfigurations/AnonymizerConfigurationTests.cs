using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    public class AnonymizerConfigurationTests
    {
        [Fact]
        public void GivenAnEmptyConfig_GenerateDefaultParametersIfNotConfigured_DefaultValueShouldBeAdded()
        {
            var configuration = new AnonymizerConfiguration();
            configuration.GenerateDefaultParametersIfNotConfigured();
            Assert.NotNull(configuration.ParameterConfiguration);
            Assert.Equal(32, configuration.ParameterConfiguration.DateShiftKey.Length);

            configuration = new AnonymizerConfiguration() { ParameterConfiguration = new ParameterConfiguration() };
            configuration.GenerateDefaultParametersIfNotConfigured();
            Assert.NotNull(configuration.ParameterConfiguration);
            Assert.Equal(32, configuration.ParameterConfiguration.DateShiftKey.Length);
        }

        [Fact]
        public void GivenConfigWithParameter_GenerateDefaultParametersIfNotConfigured_ParametersShouldNotOverwrite()
        {
            var configuration = new AnonymizerConfiguration();
            configuration.GenerateDefaultParametersIfNotConfigured();
            Assert.NotNull(configuration.ParameterConfiguration);
            Assert.Equal(32, configuration.ParameterConfiguration.DateShiftKey.Length);

            configuration = new AnonymizerConfiguration()
            {
                ParameterConfiguration = new ParameterConfiguration()
                {
                    DateShiftKey = "123"
                }
            };

            configuration.GenerateDefaultParametersIfNotConfigured();
            Assert.Equal("123", configuration.ParameterConfiguration.DateShiftKey);
        }

        [Fact]
        public void GivenEmptyDateShiftKey_WhenScopeIsResource_Validate_ShouldThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftKey = "",
                DateShiftScope = DateShiftScope.Resource,
                DateShiftFixedOffsetInDays = null
            };

            Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
        }

        [Fact]
        public void GivenEmptyDateShiftKey_WhenScopeIsFile_Validate_ShouldThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftKey = "",
                DateShiftScope = DateShiftScope.File,
                DateShiftFixedOffsetInDays = null
            };

            Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
        }

        [Fact]
        public void GivenEmptyDateShiftKey_WhenScopeIsFolder_Validate_ShouldThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftKey = "",
                DateShiftScope = DateShiftScope.Folder,
                DateShiftFixedOffsetInDays = null
            };

            Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
        }

        [Fact]
        public void GivenEmptyDateShiftKey_WhenFixedOffsetProvided_Validate_ShouldNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftKey = "",
                DateShiftScope = DateShiftScope.Resource,
                DateShiftFixedOffsetInDays = 7
            };

            // Should not throw - fixed offset is a valid exemption from requiring a key
            config.Validate();
        }

        [Fact]
        public void GivenNonEmptyDateShiftKey_WhenScopeIsResource_Validate_ShouldNotThrow()
        {
            var config = new ParameterConfiguration
            {
                DateShiftKey = "valid-date-shift-key-for-testing",
                DateShiftScope = DateShiftScope.Resource,
                DateShiftFixedOffsetInDays = null
            };

            // Should not throw - key is present
            config.Validate();
        }
    }
}
