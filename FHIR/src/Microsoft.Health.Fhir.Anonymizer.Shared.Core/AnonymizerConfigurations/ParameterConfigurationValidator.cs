using System;
using System.Collections.Generic;
using System.Security;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    /// <summary>
    /// Static validator for <see cref="ParameterConfiguration"/>.
    /// Validates security constraints, placeholder key detection, offset range checks,
    /// and scope-specific key requirements.
    /// </summary>
    /// <remarks>
    /// A null <see cref="ParameterConfiguration"/> is explicitly valid and returns silently.
    /// Null means the outer configuration omitted the parameters block entirely, which is
    /// permitted — validation at the parameter level is intentionally skipped in that case
    /// (Fail-Secure principle: missing config is safer than invalid config).
    /// </remarks>
    public static class ParameterConfigurationValidator
    {
        private static readonly ILogger s_logger = AnonymizerLogging.CreateLogger<ParameterConfigurationValidator>();

        /// <summary>
        /// Security guidance emitted when a placeholder or weak key is detected.
        /// Extracted as a constant to ensure consistent messaging across all key-type
        /// checks (cryptoHashKey, encryptKey, dateShiftKey) and to prevent silent
        /// divergence between the two call sites.
        /// </summary>
        private static readonly string s_keyGenerationGuidance =
            "TO GENERATE A SECURE KEY:\n" +
            "  Linux/macOS:   openssl rand -base64 32\n" +
            "  Windows:       pwsh -Command \"[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))\"\n" +
            "  .NET:          var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));\n\n" +
            "BEST PRACTICES:\n" +
            "  - Never commit actual keys to version control\n" +
            "  - Use environment variables: Environment.GetEnvironmentVariable(\"CRYPTO_KEY\")\n" +
            "  - Use Azure Key Vault, AWS Secrets Manager, or similar for production\n" +
            "  - Rotate keys periodically according to your security policy\n" +
            "  - Use different keys for different environments (dev/staging/production)\n";

        /// <summary>
        /// Valid AES key sizes in bits. AES supports 128-bit (16 bytes), 192-bit (24 bytes),
        /// and 256-bit (32 bytes) keys. Static to avoid re-allocating on every validation call.
        /// </summary>
        private static readonly HashSet<int> s_validAesKeySizeBits = new HashSet<int> { 128, 192, 256 };

        /// <summary>
        /// Validates the given <paramref name="config"/>.
        /// A null value is valid and returns silently.
        /// </summary>
        /// <param name="config">The configuration to validate, or null.</param>
        /// <exception cref="AnonymizerConfigurationException">
        /// Thrown when the configuration contains invalid settings such as an out-of-range
        /// date-shift offset or a missing DateShiftKey for the configured scope.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Thrown when a key value is a known placeholder, whitespace-only, or otherwise weak.
        /// </exception>
        public static void Validate(ParameterConfiguration config)
        {
            if (config == null)
            {
                // null ParameterConfiguration is valid: it means no parameter-level configuration
                // was provided and all parameter validation is intentionally skipped.
                return;
            }

            // SECURITY: Check for placeholder cryptographic keys
            ValidateKeyParameter(config.CryptoHashKey, "cryptoHashKey", "cryptographic hash");
            ValidateKeyParameter(config.EncryptKey, "encryptKey", "encryption");
            ValidateKeyParameter(config.DateShiftKey, "dateShiftKey", "date shift");

            // SECURITY: Enforce minimum length for CryptoHashKey
            if (!string.IsNullOrWhiteSpace(config.CryptoHashKey) &&
                config.CryptoHashKey.Trim().Length < ParameterDefaults.MinCryptoHashKeyLength)
            {
                throw new SecurityException(
                    $"SECURITY ERROR: The cryptoHashKey is too short ({config.CryptoHashKey.Trim().Length} characters). " +
                    $"A minimum of {ParameterDefaults.MinCryptoHashKeyLength} characters is required to ensure " +
                    "adequate entropy for HMAC-SHA256 operations.\n\n" +
                    s_keyGenerationGuidance);
            }

            // SECURITY: Validate EncryptKey is a valid AES key size (128/192/256 bits)
            ValidateEncryptKeySize(config.EncryptKey);

            // Validate fixed date-shift offset range
            ValidateDateShiftFixedOffsetInDays(config);

            // SECURITY: Validate DateShiftKey presence relative to DateShiftScope.
            // Resource scope also requires a key because the HMAC-based date shift uses
            // (resourceId + dateShiftKey) as its input. Without a key, the shift is determined
            // solely by the resource ID, which is often predictable or publicly known. An attacker
            // who knows the resource ID can recompute the shift and reverse the date offset,
            // enabling re-identification. A secret key prevents this.
            ValidateDateShiftKeyForScope(config);

            // Validate differential privacy settings
            if (config.DifferentialPrivacySettings != null)
            {
                ValidateDifferentialPrivacySettings(config.DifferentialPrivacySettings);
            }

            // Validate k-anonymity settings
            if (config.KAnonymitySettings != null)
            {
                ValidateKAnonymitySettings(config.KAnonymitySettings);
            }
        }

        /// <summary>
        /// Validate that the encrypt key size is a valid AES key size (128, 192, or 256 bits).
        /// Only validates when encryptKey is non-null and non-empty.
        /// </summary>
        private static void ValidateEncryptKeySize(string encryptKey)
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

        /// <summary>
        /// Validate that DateShiftFixedOffsetInDays, when provided, falls within the allowed range.
        /// A null value is always valid.
        /// </summary>
        private static void ValidateDateShiftFixedOffsetInDays(ParameterConfiguration config)
        {
            if (!config.DateShiftFixedOffsetInDays.HasValue)
            {
                return;
            }

            int offset = config.DateShiftFixedOffsetInDays.Value;
            if (offset < ParameterDefaults.MinDateShiftOffsetDays ||
                offset > ParameterDefaults.MaxDateShiftOffsetDays)
            {
                throw new AnonymizerConfigurationException(
                    $"The dateShiftFixedOffsetInDays value {offset} is out of the allowed range " +
                    $"[{ParameterDefaults.MinDateShiftOffsetDays}, {ParameterDefaults.MaxDateShiftOffsetDays}]. " +
                    "Provide a value between -365 and 365 days, or omit the setting to use the " +
                    "deterministic key-based date shift.");
            }
        }

        /// <summary>
        /// Validate a key parameter does not contain placeholder values or consist solely of whitespace.
        /// SECURITY CRITICAL: Prevents use of example/template keys and whitespace-only values in production.
        /// </summary>
        private static void ValidateKeyParameter(string keyValue, string parameterName, string keyType)
        {
            if (string.IsNullOrEmpty(keyValue))
            {
                return; // Empty/null keys are allowed if the feature is not used
            }

            // SECURITY: Reject whitespace-only keys - they provide no entropy
            if (string.IsNullOrWhiteSpace(keyValue))
            {
                throw new SecurityException(
                    $"SECURITY ERROR: Whitespace-only {keyType} key detected in '{parameterName}'. " +
                    "A key consisting entirely of whitespace characters provides no entropy and must not be used. " +
                    "Generate a cryptographically secure random key using: openssl rand -base64 32");
            }

            // Trim and convert to uppercase for case-insensitive comparison
            var normalizedKey = keyValue.Trim().ToUpperInvariant();

            // Check against all dangerous placeholder patterns.
            // Normalize both sides to upper-case for case-insensitive comparison.
            foreach (var pattern in ParameterDefaults.DangerousPlaceholderPatterns)
            {
                if (normalizedKey.Contains(pattern.ToUpperInvariant(), StringComparison.Ordinal))
                {
                    throw new SecurityException(
                        $"SECURITY ERROR: Placeholder {keyType} key detected in '{parameterName}'.\n\n" +
                        $"The configuration contains a placeholder value ('{pattern}') that must be replaced " +
                        "with a cryptographically secure key before use.\n\n" +
                        s_keyGenerationGuidance +
                        "SECURITY WARNING: Using placeholder keys in production:\n" +
                        "  - Compromises cryptographic operations\n" +
                        "  - May lead to predictable hash values\n" +
                        "  - Enables re-identification attacks\n" +
                        "  - Violates privacy guarantees\n");
                }
            }

            // Additional check: warn on very short keys
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
        /// Validates that a non-empty DateShiftKey is present for ALL DateShiftScope values
        /// when DateShiftFixedOffsetInDays is not set.
        ///
        /// SECURITY: Resource scope also requires a key because the HMAC-based date shift uses
        /// (resourceId + dateShiftKey) as its input. Without a key, the shift is determined solely
        /// by the resource ID, which is often predictable or publicly known. An attacker who knows
        /// the resource ID can recompute the shift and reverse the date offset, enabling
        /// re-identification. A secret key prevents this.
        ///
        /// File and Folder scopes additionally require a key for consistency: all resources
        /// in the same file or folder must receive the same deterministic shift.
        /// </summary>
        private static void ValidateDateShiftKeyForScope(ParameterConfiguration config)
        {
            var scope = config.DateShiftScope;

            if (string.IsNullOrEmpty(config.DateShiftKey) &&
                !config.DateShiftFixedOffsetInDays.HasValue)
            {
                throw new AnonymizerConfigurationException(
                    $"A dateShiftKey is required when dateShiftScope is '{scope}' and dateShiftFixedOffsetInDays is not set. " +
                    "Provide a non-empty dateShiftKey, or set dateShiftFixedOffsetInDays to use a fixed date-shift offset instead.");
            }
        }

        /// <summary>
        /// Validate differential privacy configuration parameters.
        /// </summary>
        private static void ValidateDifferentialPrivacySettings(DifferentialPrivacyParameterConfiguration settings)
        {
            if (settings.Epsilon <= 0)
            {
                throw new ArgumentException("Differential privacy epsilon must be greater than 0");
            }

            if (settings.Epsilon > 10.0)
            {
                throw new ArgumentException(
                    $"Differential privacy epsilon value {settings.Epsilon} exceeds maximum of 10.0. " +
                    "High epsilon values provide minimal privacy protection. See configuration comments for guidance.");
            }

            if (settings.Epsilon > 1.0)
            {
                s_logger.LogWarning(
                    $"Differential privacy epsilon value {settings.Epsilon} is high (>1.0). " +
                    "This provides weaker privacy guarantees. Consider using epsilon <= 1.0 for moderate privacy " +
                    "or epsilon <= 0.1 for strong privacy (NIST SP 800-188 guidance for health data).");
            }

            if (settings.Delta < 0 || settings.Delta > 1)
            {
                throw new ArgumentException("Differential privacy delta must be between 0 and 1");
            }

            if (settings.Sensitivity <= 0)
            {
                throw new ArgumentException("Differential privacy sensitivity must be greater than 0");
            }

            if (settings.MaxCumulativeEpsilon <= 0)
            {
                throw new ArgumentException("Differential privacy maxCumulativeEpsilon must be greater than 0");
            }
        }

        /// <summary>
        /// Validate k-anonymity configuration parameters.
        /// </summary>
        private static void ValidateKAnonymitySettings(KAnonymityParameterConfiguration settings)
        {
            if (settings.KValue < 2)
            {
                throw new ArgumentException(
                    $"K-anonymity k-value must be at least 2 (provided: {settings.KValue}). " +
                    "k=1 provides no privacy protection.");
            }

            if (settings.KValue == 2)
            {
                s_logger.LogWarning(
                    "K-anonymity k-value is 2 (minimal). Consider k >= 5 for better privacy protection " +
                    "(recommended by HIPAA Safe Harbor guidance).");
            }

            if (settings.SuppressionThreshold < 0 || settings.SuppressionThreshold > 1)
            {
                throw new ArgumentException(
                    "K-anonymity suppression threshold must be between 0 and 1 (represents percentage)");
            }
        }
    }
}
