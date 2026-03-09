using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests
{
    public class ParameterConfigurationTests
    {
        [Fact]
        public void GivenEncryptKeyOf16Bytes_WhenValidate_NoExceptionThrown()
        {
            var config = new ParameterConfiguration
            {
                // 16 distinct ASCII chars = 128 bits, valid AES key size.
                // Must NOT be all-same-character (would trigger the weak-key guard).
                EncryptKey = "abcdefghijklmnop"
            };
            config.Validate(); // should not throw
        }

        [Fact]
        public void GivenEncryptKeyOf24Bytes_WhenValidate_NoExceptionThrown()
        {
            var config = new ParameterConfiguration
            {
                // 24 distinct ASCII chars = 192 bits, valid AES key size.
                EncryptKey = "abcdefghijklmnopqrstuvwx"
            };
            config.Validate(); // should not throw
        }

        [Fact]
        public void GivenEncryptKeyOf32Bytes_WhenValidate_NoExceptionThrown()
        {
            var config = new ParameterConfiguration
            {
                // 32 distinct ASCII chars = 256 bits, valid AES key size.
                EncryptKey = "abcdefghijklmnopqrstuvwxyz012345"
            };
            config.Validate(); // should not throw
        }

        [Fact]
        public void GivenEncryptKeyOf20Bytes_WhenValidate_ExceptionThrown()
        {
            var config = new ParameterConfiguration
            {
                // Same 20-char key used in configuration-invalid-encryptkey.json.
                // 20 bytes = 160 bits, not a valid AES key size.
                EncryptKey = "01234567890123456789"
            };
            Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
        }

        [Fact]
        public void GivenEncryptKeyOf8Bytes_WhenValidate_ExceptionThrown()
        {
            var config = new ParameterConfiguration
            {
                // 8 distinct ASCII chars = 64 bits, not a valid AES key size.
                // Must NOT be all-same-character (would trigger SecurityException, not
                // AnonymizerConfigurationException, so the Assert.Throws would fail).
                EncryptKey = "abcdefgh"
            };
            Assert.Throws<AnonymizerConfigurationException>(() => config.Validate());
        }

        [Fact]
        public void GivenNullEncryptKey_WhenValidate_NoExceptionThrown()
        {
            var config = new ParameterConfiguration
            {
                EncryptKey = null
            };
            config.Validate(); // should not throw
        }

        [Fact]
        public void GivenEmptyEncryptKey_WhenValidate_NoExceptionThrown()
        {
            var config = new ParameterConfiguration
            {
                EncryptKey = string.Empty
            };
            config.Validate(); // should not throw
        }
    }
}
