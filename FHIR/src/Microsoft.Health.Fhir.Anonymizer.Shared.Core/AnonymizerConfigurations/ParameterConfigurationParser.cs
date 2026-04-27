using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    /// <summary>
    /// Centralizes all JSON parsing and deserialization logic for
    /// <see cref="ParameterConfiguration"/> and its nested types.
    /// All parsing errors are wrapped in <see cref="AnonymizerConfigurationException"/>
    /// to preserve the fail-secure principle.
    /// </summary>
    public sealed class ParameterConfigurationParser
    {
        /// <summary>
        /// Explicitly deserializes a <see cref="ParameterConfiguration"/> from a JObject
        /// representing the 'parameters' block. Returns <see langword="null"/> when
        /// <paramref name="parametersToken"/> is null.
        /// </summary>
        public static ParameterConfiguration Parse(JObject parametersToken)
        {
            if (parametersToken == null)
            {
                return null;
            }

            try
            {
                var config = new ParameterConfiguration();

                config.DateShiftKey = ReadString(parametersToken, "dateShiftKey");

                var dateShiftScopeToken = parametersToken["dateShiftScope"];
                if (dateShiftScopeToken != null && dateShiftScopeToken.Type != JTokenType.Null)
                {
                    var scopeValue = dateShiftScopeToken.Value<string>();
                    if (!Enum.TryParse<DateShiftScope>(scopeValue, ignoreCase: true, out var scope))
                    {
                        throw new AnonymizerConfigurationException(
                            "Invalid dateShiftScope value. Allowed values are: resource, file, folder.");
                    }
                    config.DateShiftScope = scope;
                }

                var fixedOffsetToken = parametersToken["dateShiftFixedOffsetInDays"];
                if (fixedOffsetToken != null && fixedOffsetToken.Type != JTokenType.Null)
                {
                    try
                    {
                        config.DateShiftFixedOffsetInDays = fixedOffsetToken.Value<int>();
                    }
                    catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
                    {
                        throw new AnonymizerConfigurationException("dateShiftFixedOffsetInDays must be an integer value.", ex);
                    }
                }

                config.CryptoHashKey = ReadString(parametersToken, "cryptoHashKey");
                config.EncryptKey = ReadString(parametersToken, "encryptKey");

                // Validate AES key size by UTF-8 byte count: AES requires exactly
                // 16 bytes (AES-128), 24 bytes (AES-192), or 32 bytes (AES-256).
                // Byte count is used rather than character count to correctly handle
                // multi-byte UTF-8 characters that would silently undercount with .Length.
                if (config.EncryptKey != null)
                {
                    var encryptKeyByteCount = Encoding.UTF8.GetByteCount(config.EncryptKey);
                    if (encryptKeyByteCount != 16
                        && encryptKeyByteCount != 24
                        && encryptKeyByteCount != 32)
                    {
                        throw new AnonymizerConfigurationException(
                            "EncryptKey must be 16, 24, or 32 bytes (UTF-8 encoded) for AES-128, AES-192, or AES-256.");
                    }
                }

                config.EnablePartialAgesForRedact = ReadBool(parametersToken, "enablePartialAgesForRedact");
                config.EnablePartialDatesForRedact = ReadBool(parametersToken, "enablePartialDatesForRedact");
                config.EnablePartialZipCodesForRedact = ReadBool(parametersToken, "enablePartialZipCodesForRedact");
                config.RestrictedZipCodeTabulationAreas = ParseRestrictedZipCodes(parametersToken["restrictedZipCodeTabulationAreas"]);
                config.KAnonymitySettings = ParseKAnonymitySettings(parametersToken["kAnonymitySettings"]);
                config.DifferentialPrivacySettings = ParseDifferentialPrivacySettings(parametersToken["differentialPrivacySettings"]);

                // Validate customSettings is a JSON object if present — fail-secure guard
                // prevents a silent null result from a non-object token (e.g. array or string).
                var csToken = parametersToken["customSettings"];
                if (csToken != null && csToken.Type != JTokenType.Null)
                {
                    if (csToken is not JObject csObj)
                    {
                        throw new AnonymizerConfigurationException("'customSettings' must be a JSON object.");
                    }
                    config.CustomSettings = csObj;
                }

                return config;
            }
            catch (AnonymizerConfigurationException)
            {
                throw;
            }
            catch (JsonException ex)
            {
                throw new AnonymizerConfigurationException("Failed to parse parameters configuration.", ex);
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException || ex is InvalidCastException)
            {
                throw new AnonymizerConfigurationException("Failed to parse parameters configuration: invalid value type.", ex);
            }
        }

        /// <summary>
        /// Parses a <see cref="ParameterConfiguration"/> from a JSON string representing
        /// only the parameters block.
        /// Enforces <see cref="DuplicatePropertyNameHandling.Error"/> so that duplicate JSON
        /// keys cause an <see cref="AnonymizerConfigurationException"/>.
        /// Returns <see langword="null"/> when <paramref name="parametersJson"/> is null or whitespace.
        /// </summary>
        public static ParameterConfiguration ParseFromJson(string parametersJson)
        {
            if (string.IsNullOrWhiteSpace(parametersJson))
            {
                return null;
            }

            try
            {
                var loadSettings = new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                };
                var token = JObject.Parse(parametersJson, loadSettings);
                return Parse(token);
            }
            catch (AnonymizerConfigurationException)
            {
                throw;
            }
            catch (JsonException ex)
            {
                throw new AnonymizerConfigurationException("Failed to parse parameters JSON.", ex);
            }
        }

        /// <summary>
        /// Extracts the 'parameters' sub-token from a full anonymizer configuration JSON string
        /// and delegates to <see cref="Parse(JObject)"/>.
        /// Returns <see langword="null"/> when the 'parameters' key is absent or its value is
        /// JSON null, and when <paramref name="fullConfigJson"/> is null or whitespace.
        /// Enforces <see cref="DuplicatePropertyNameHandling.Error"/> on the full document.
        /// </summary>
        /// <exception cref="AnonymizerConfigurationException">
        /// Thrown when the 'parameters' value is present but is not a JSON object.
        /// </exception>
        public static ParameterConfiguration ParseFromAnonymizerConfigJson(string fullConfigJson)
        {
            if (string.IsNullOrWhiteSpace(fullConfigJson))
            {
                return null;
            }

            try
            {
                var loadSettings = new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                };
                var root = JObject.Parse(fullConfigJson, loadSettings);
                var rawParametersToken = root["parameters"];
                if (rawParametersToken == null || rawParametersToken.Type == JTokenType.Null)
                {
                    return null;
                }
                if (rawParametersToken is not JObject parametersToken)
                {
                    throw new AnonymizerConfigurationException("'parameters' must be a JSON object.");
                }
                return Parse(parametersToken);
            }
            catch (AnonymizerConfigurationException)
            {
                throw;
            }
            catch (JsonException ex)
            {
                throw new AnonymizerConfigurationException("Failed to parse anonymizer configuration JSON.", ex);
            }
        }

        private static KAnonymityParameterConfiguration ParseKAnonymitySettings(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            if (token is not JObject obj)
            {
                throw new AnonymizerConfigurationException("kAnonymitySettings must be a JSON object.");
            }

            var config = new KAnonymityParameterConfiguration();

            var kValueToken = obj["kValue"];
            if (kValueToken != null && kValueToken.Type != JTokenType.Null)
            {
                config.KValue = kValueToken.Value<int>();
                if (config.KValue < 2)
                {
                    throw new AnonymizerConfigurationException("k-anonymity kValue must be at least 2.");
                }
            }

            var suppressionToken = obj["suppressionThreshold"];
            if (suppressionToken != null && suppressionToken.Type != JTokenType.Null)
            {
                config.SuppressionThreshold = suppressionToken.Value<double>();
                if (config.SuppressionThreshold < 0.0 || config.SuppressionThreshold > 1.0)
                {
                    throw new AnonymizerConfigurationException("suppressionThreshold must be in range [0.0, 1.0].");
                }
            }

            var quasiIdentifiersToken = obj["quasiIdentifiers"];
            if (quasiIdentifiersToken != null && quasiIdentifiersToken.Type != JTokenType.Null)
            {
                if (quasiIdentifiersToken.Type != JTokenType.Array)
                {
                    throw new AnonymizerConfigurationException("'quasiIdentifiers' must be a JSON array.");
                }
                config.QuasiIdentifiers = quasiIdentifiersToken.ToObject<List<string>>();
            }

            var hierarchiesToken = obj["generalizationHierarchies"];
            if (hierarchiesToken != null && hierarchiesToken.Type != JTokenType.Null)
            {
                if (hierarchiesToken.Type != JTokenType.Object)
                {
                    throw new AnonymizerConfigurationException("'generalizationHierarchies' must be a JSON object.");
                }
                config.GeneralizationHierarchies = hierarchiesToken.ToObject<Dictionary<string, List<string>>>();
            }

            return config;
        }

        /// <summary>
        /// Parses differential privacy settings from a JToken.
        /// The <c>mechanism</c> field, when present, must be one of: <c>laplace</c>, <c>gaussian</c>
        /// (case-insensitive). Numeric bounds: epsilon and sensitivity must be strictly greater
        /// than 0; delta must be in [0, 1]; maxCumulativeEpsilon must be strictly greater than 0.
        /// </summary>
        private static DifferentialPrivacyParameterConfiguration ParseDifferentialPrivacySettings(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            if (token is not JObject obj)
            {
                throw new AnonymizerConfigurationException("differentialPrivacySettings must be a JSON object.");
            }

            var config = new DifferentialPrivacyParameterConfiguration();

            var epsilonToken = obj["epsilon"];
            if (epsilonToken != null && epsilonToken.Type != JTokenType.Null)
            {
                config.Epsilon = epsilonToken.Value<double>();
                if (config.Epsilon <= 0)
                {
                    throw new AnonymizerConfigurationException("Differential privacy epsilon must be strictly greater than 0.");
                }
            }

            var mechanismToken = obj["mechanism"];
            if (mechanismToken != null && mechanismToken.Type != JTokenType.Null)
            {
                config.Mechanism = mechanismToken.Value<string>();
                // Whitelist: only 'laplace' and 'gaussian' are valid mechanism values.
                if (!string.Equals(config.Mechanism, "laplace", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(config.Mechanism, "gaussian", StringComparison.OrdinalIgnoreCase))
                {
                    throw new AnonymizerConfigurationException("'mechanism' must be one of: laplace, gaussian.");
                }
            }

            var deltaToken = obj["delta"];
            if (deltaToken != null && deltaToken.Type != JTokenType.Null)
            {
                config.Delta = deltaToken.Value<double>();
                if (config.Delta < 0 || config.Delta > 1)
                {
                    throw new AnonymizerConfigurationException("Differential privacy delta must be in range [0, 1].");
                }
            }

            var sensitivityToken = obj["sensitivity"];
            if (sensitivityToken != null && sensitivityToken.Type != JTokenType.Null)
            {
                config.Sensitivity = sensitivityToken.Value<double>();
                if (config.Sensitivity <= 0)
                {
                    throw new AnonymizerConfigurationException(
                        "Differential privacy sensitivity must be strictly greater than 0.");
                }
            }

            var maxCumulativeEpsilonToken = obj["maxCumulativeEpsilon"];
            if (maxCumulativeEpsilonToken != null && maxCumulativeEpsilonToken.Type != JTokenType.Null)
            {
                config.MaxCumulativeEpsilon = maxCumulativeEpsilonToken.Value<double>();
                if (config.MaxCumulativeEpsilon <= 0)
                {
                    throw new AnonymizerConfigurationException(
                        "maxCumulativeEpsilon must be strictly greater than 0.");
                }
            }

            var useAdvancedCompositionToken = obj["useAdvancedComposition"];
            if (useAdvancedCompositionToken != null && useAdvancedCompositionToken.Type != JTokenType.Null)
            {
                config.UseAdvancedComposition = useAdvancedCompositionToken.Value<bool>();
            }

            var privacyBudgetToken = obj["privacyBudgetTrackingEnabled"];
            if (privacyBudgetToken != null && privacyBudgetToken.Type != JTokenType.Null)
            {
                config.PrivacyBudgetTrackingEnabled = privacyBudgetToken.Value<bool>();
            }

            var clippingToken = obj["clippingEnabled"];
            if (clippingToken != null && clippingToken.Type != JTokenType.Null)
            {
                config.ClippingEnabled = clippingToken.Value<bool>();
            }

            return config;
        }

        private static List<string> ParseRestrictedZipCodes(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            if (token.Type != JTokenType.Array)
            {
                throw new AnonymizerConfigurationException("restrictedZipCodeTabulationAreas must be a JSON array.");
            }

            try
            {
                return token.ToObject<List<string>>();
            }
            catch (JsonSerializationException ex)
            {
                throw new AnonymizerConfigurationException(
                    "restrictedZipCodeTabulationAreas must be an array of strings.", ex);
            }
        }

        /// <summary>
        /// Reads a string value from a JSON object by key.
        /// Returns <see langword="null"/> when the key is absent or its value is JSON null.
        /// Throws <see cref="AnonymizerConfigurationException"/> when the token is present
        /// and non-null but is not a JSON string type — prevents Newtonsoft.Json silent
        /// coercion of integers, booleans, or floats into strings for sensitive fields
        /// such as encryptKey, cryptoHashKey, and dateShiftKey.
        /// </summary>
        private static string ReadString(JObject obj, string key)
        {
            var token = obj[key];
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }
            if (token.Type != JTokenType.String)
            {
                throw new AnonymizerConfigurationException($"'{key}' must be a string value.");
            }
            return token.Value<string>();
        }

        private static bool ReadBool(JObject obj, string key)
        {
            var token = obj[key];
            if (token == null || token.Type == JTokenType.Null)
            {
                return false;
            }
            return token.Value<bool>();
        }
    }
}
