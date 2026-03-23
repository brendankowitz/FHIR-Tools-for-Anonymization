using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    /// <summary>
    /// Static utility class that encapsulates all cryptographic key security validation logic
    /// for anonymization configuration parameters.
    ///
    /// SECURITY: This class enforces that keys are not placeholder values, not whitespace-only,
    /// not obviously weak values, and meet minimum length/size requirements.
    /// </summary>
    public static class CryptographicKeyValidator
    {
        private static readonly ILogger s_logger = AnonymizerLogging.CreateLogger<ParameterConfiguration>();

        /// <summary>
        /// Valid AES key sizes in bits. Used to validate EncryptKey without allocating an Aes instance.
        /// AES supports 128-bit (16 bytes), 192-bit (24 bytes), and 256-bit (32 bytes) keys.
        /// </summary>
        private static readonly HashSet<int> s_validAesKeySizeBits = new HashSet<int> { 128, 192, 256 };

        /// <summary>
        /// Dangerous placeholder patterns that must be rejected.
        /// </summary>
        private static readonly string[] s_dangerousPlaceholderPatterns = new[]
        {
            "$HMAC_KEY",
            "YOUR_KEY_HERE",
            "YOUR_SECURE_KEY",
            "YOUR_ENCRYPTION_KEY",
            "PLACEHOLDER",
            "CHANGE_ME",
            "CHANGEME",
            "REPLACE_ME",
            "EXAMPLE_KEY",
            "TEST_KEY",
            "SAMPLE_KEY",
            "INSERT_KEY_HERE",
            "<YOUR_KEY>",
            "[YOUR_KEY]",
            "{{YOUR_KEY}}",
            "TODO",
            "FIXME"
        };

        /// <summary>
        /// Validate a key parameter doesn't contain placeholder values or consist solely of whitespace.
        /// SECURITY CRITICAL: Prevents use of example/template keys and whitespace-only values in production.
        /// </summary>
        /// <param name="keyValue">The key value to validate.</param>
        /// <param name="parameterName">The configuration parameter name (for error messages).</param>
        /// <param name="keyType">Human-readable key type description (for error messages).</param>
        public static void ValidateKeyParameter(string keyValue, string parameterName, string keyType)
        {
            if (string.IsNullOrEmpty(keyValue))
            {
                return; // Empty/null keys are allowed if the feature is not used
            }

            // SECURITY: Reject whitespace-only keys — they provide no entropy
            if (string.IsNullOrWhiteSpace(keyValue))
            {
                throw new SecurityException(
                    $"SECURITY ERROR: Whitespace-only {keyType} key detected in '{parameterName}'. " +
                    "A key consisting entirely of whitespace characters provides no entropy and must not be used. " +
                    "Generate a cryptographically secure random key using: openssl rand -base64 32");
            }

            // Trim and convert to uppercase for case-insensitive comparison
            var normalizedKey = keyValue.Trim().ToUpperInvariant();

            // Check against all dangerous placeholder patterns
            foreach (var pattern in s_dangerousPlaceholderPatterns)
            {
                if (normalizedKey.Contains(pattern))
                {
                    throw new SecurityException(
                        $"SECURITY ERROR: Placeholder {keyType} key detected in '{parameterName}'.\n\n" +
                        $"The configuration contains a placeholder value ('{pattern}') that must be replaced " +
                        "with a cryptographically secure key before use.\n\n" +
                        "TO GENERATE A SECURE KEY:\n" +
                        "  Linux/macOS:   openssl rand -base64 32\n" +
                        "  Windows:       pwsh -Command \"[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))\"\n" +
                        "  .NET:          var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));\n\n" +
                        "SECURITY WARNING: Using placeholder keys in production:\n" +
                        "  - Compromises cryptographic operations\n" +
                        "  - May lead to predictable hash values\n" +
                        "  - Enables re-identification attacks\n" +
                        "  - Violates privacy guarantees\n\n" +
                        "BEST PRACTICES:\n" +
                        "  - Never commit actual keys to version control\n" +
                        "  - Use environment variables: Environment.GetEnvironmentVariable(\"CRYPTO_KEY\")\n" +
                        "  - Use Azure Key Vault, AWS Secrets Manager, or similar for production\n" +
                        "  - Rotate keys periodically according to your security policy\n" +
                        "  - Use different keys for different environments (dev/staging/production)\n");
                }
            }

            // Additional checks for weak or test keys
            if (keyValue.Length < 16)
            {
                s_logger.LogWarning(
                    $"The {keyType} key in '{parameterName}' is very short ({keyValue.Length} characters). " +
                    "Recommended minimum is 32 bytes (44 characters in Base64). " +
                    "Short keys provide inadequate security and may be vulnerable to brute force attacks.");
            }

            // Check for obviously weak patterns
            if (keyValue.Equals("12345678", StringComparison.Ordinal) ||
                keyValue.Equals("password", StringComparison.OrdinalIgnoreCase) ||
                keyValue.Equals("secret", StringComparison.OrdinalIgnoreCase) ||
                keyValue.Equals("key", StringComparison.OrdinalIgnoreCase) ||
                keyValue.All(c => c == keyValue[0])) // All same character
            {
                throw new SecurityException(
                    $"SECURITY ERROR: Weak {keyType} key detected in '{parameterName}'. " +
                    "The key appears to be a common weak value (e.g., 'password', '12345678', repeated characters). " +
                    "Generate a cryptographically secure random key using: openssl rand -base64 32");
            }
        }

        /// <summary>
        /// Validate that the encrypt key size is a valid AES key size (128, 192, or 256 bits).
        /// Uses a static HashSet of valid sizes to avoid allocating an Aes instance on every call.
        /// Only validates when encryptKey is non-null and non-empty.
        /// </summary>
        /// <param name="encryptKey">The encryption key string to validate.</param>
        public static void ValidateEncryptKeySize(string encryptKey)
        {
            if (string.IsNullOrEmpty(encryptKey))
            {
                return;
            }

            var encryptKeySize = Encoding.UTF8.GetByteCount(encryptKey) * 8;
            if (!s_validAesKeySizeBits.Contains(encryptKeySize))
            {
                throw new AnonymizerConfigurationException(
                    $"Invalid encrypt key size : {encryptKeySize} bits! Please provide key sizes of 128, 192 or 256 bits.");
            }
        }
    }
}
