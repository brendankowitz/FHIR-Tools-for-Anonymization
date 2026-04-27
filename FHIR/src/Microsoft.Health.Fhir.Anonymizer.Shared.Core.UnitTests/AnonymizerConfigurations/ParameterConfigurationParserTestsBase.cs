using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations
{
    /// <summary>
    /// Shared abstract base class for ParameterConfigurationParser tests.
    /// Both the R4 and R5 test projects inherit from this class to avoid duplication.
    /// </summary>
    public abstract class ParameterConfigurationParserTestsBase
    {
        [Fact]
        public void ParseFromJson_ValidMinimalConfig_ReturnsExpectedDefaults()
        {
            var json = "{}";
            var result = ParameterConfigurationParser.ParseFromJson(json);
            Assert.NotNull(result);
            Assert.Null(result.DateShiftKey);
            Assert.Null(result.CryptoHashKey);
            Assert.False(result.EnablePartialAgesForRedact);
            Assert.False(result.EnablePartialDatesForRedact);
            Assert.False(result.EnablePartialZipCodesForRedact);
            Assert.Null(result.RestrictedZipCodeTabulationAreas);
            Assert.Null(result.KAnonymitySettings);
            Assert.Null(result.DifferentialPrivacySettings);
        }

        [Fact]
        public void ParseFromJson_AllFieldsSet_MapsCorrectly()
        {
            var json = "{\"dateShiftKey\": \"myDateShiftKey\", \"dateShiftScope\": \"resource\", \"cryptoHashKey\": \"myCryptoHashKey1234567890abcdef\", \"encryptKey\": \"1234567890123456\", \"enablePartialAgesForRedact\": true, \"enablePartialDatesForRedact\": true, \"enablePartialZipCodesForRedact\": true, \"restrictedZipCodeTabulationAreas\": [\"123\", \"456\"]}";
            var result = ParameterConfigurationParser.ParseFromJson(json);
            Assert.NotNull(result);
            Assert.Equal("myDateShiftKey", result.DateShiftKey);
            Assert.Equal(DateShiftScope.Resource, result.DateShiftScope);
            Assert.Equal("myCryptoHashKey1234567890abcdef", result.CryptoHashKey);
            Assert.Equal("1234567890123456", result.EncryptKey);
            Assert.True(result.EnablePartialAgesForRedact);
            Assert.True(result.EnablePartialDatesForRedact);
            Assert.True(result.EnablePartialZipCodesForRedact);
            Assert.NotNull(result.RestrictedZipCodeTabulationAreas);
            Assert.Equal(2, result.RestrictedZipCodeTabulationAreas.Count);
            Assert.Contains("123", result.RestrictedZipCodeTabulationAreas);
            Assert.Contains("456", result.RestrictedZipCodeTabulationAreas);
        }

        [Fact]
        public void Parse_MalformedJson_ThrowsAnonymizerConfigurationException()
        {
            Assert.Throws<AnonymizerConfigurationException>(() =>
                ParameterConfigurationParser.ParseFromJson("{invalid json"));
        }

        [Fact]
        public void Parse_NullParametersToken_ReturnsNull()
        {
            var result = ParameterConfigurationParser.Parse(null);
            Assert.Null(result);
        }

        [Fact]
        public void ParseFromJson_NullOrWhitespace_ReturnsNull()
        {
            Assert.Null(ParameterConfigurationParser.ParseFromJson(null));
            Assert.Null(ParameterConfigurationParser.ParseFromJson(""));
            Assert.Null(ParameterConfigurationParser.ParseFromJson("   "));
        }

        [Fact]
        public void Parse_DateShiftFixedOffsetProvided_IsStoredCorrectly()
        {
            var json = "{\"dateShiftFixedOffsetInDays\": 30}";
            var result = ParameterConfigurationParser.ParseFromJson(json);
            Assert.NotNull(result);
            Assert.Equal(30, result.DateShiftFixedOffsetInDays);
        }

        [Fact]
        public void Parse_NegativeDateShiftFixedOffset_ParsedCorrectly()
        {
            var json = "{\"dateShiftFixedOffsetInDays\": -100}";
            var result = ParameterConfigurationParser.ParseFromJson(json);
            Assert.NotNull(result);
            Assert.Equal(-100, result.DateShiftFixedOffsetInDays);
        }

        [Fact]
        public void Parse_DateShiftFixedOffsetAbsent_IsNull()
        {
            var json = "{\"dateShiftKey\": \"somekey\"}";
            var result = ParameterConfigurationParser.ParseFromJson(json);
            Assert.Null(result.DateShiftFixedOffsetInDays);
        }

        [Fact]
        public void Parse_RedactPartialFlags_ParsedCorrectly()
        {
            var json = "{\"enablePartialAgesForRedact\": true, \"enablePartialDatesForRedact\": false, \"enablePartialZipCodesForRedact\": true}";
            var result = ParameterConfigurationParser.ParseFromJson(json);
            Assert.True(result.EnablePartialAgesForRedact);
            Assert.False(result.EnablePartialDatesForRedact);
            Assert.True(result.EnablePartialZipCodesForRedact);
        }

        [Fact]
        public void Parse_RestrictedZipCodes_ParsedAsList()
        {
            var json = "{\"restrictedZipCodeTabulationAreas\": [\"123\", \"456\", \"789\"]}";
            var result = ParameterConfigurationParser.ParseFromJson(json);
            Assert.NotNull(result.RestrictedZipCodeTabulationAreas);
            Assert.Equal(3, result.RestrictedZipCodeTabulationAreas.Count);
            Assert.Contains("123", result.RestrictedZipCodeTabulationAreas);
            Assert.Contains("456", result.RestrictedZipCodeTabulationAreas);
            Assert.Contains("789", result.RestrictedZipCodeTabulationAreas);
        }

        [Fact]
        public void Parse_EmptyRestrictedZipCodes_ParsedAsEmptyList()
        {
            var json = "{\"restrictedZipCodeTabulationAreas\": []}";
            var result = ParameterConfigurationParser.ParseFromJson(json);
            Assert.NotNull(result.RestrictedZipCodeTabulationAreas);
            Assert.Empty(result.RestrictedZipCodeTabulationAreas);
        }

        [Fact]
        public void Parse_KAnonymitySettings_ParsedWithDefaults()
        {
            var json = "{\"kAnonymitySettings\": {}}";
            var result = ParameterConfigurationParser.ParseFromJson(json);
            Assert.NotNull(result.KAnonymitySettings);
            Assert.Equal(5, result.KAnonymitySettings.KValue);
            Assert.Equal(0.3, result.KAnonymitySettings.SuppressionThreshold);
        }

        [Fact]
        public void Parse_KAnonymitySettings_CustomValues_ParsedCorrectly()
        {
            var json = "{\"kAnonymitySettings\": {\"kValue\": 10, \"suppressionThreshold\": 0.5}}";
            var result = ParameterConfigurationParser.ParseFromJson(json);
            Assert.NotNull(result.KAnonymitySettings);
            Assert.Equal(10, result.KAnonymitySettings.KValue);
            Assert.Equal(0.5, result.KAnonymitySettings.SuppressionThreshold);
        }

        [Fact]
        public void Parse_DifferentialPrivacySettings_ParsedWithDefaults()
        {
            var json = "{\"differentialPrivacySettings\": {}}";
            var result = ParameterConfigurationParser.ParseFromJson(json);
            Assert.NotNull(result.DifferentialPrivacySettings);
            Assert.Equal(1.0, result.DifferentialPrivacySettings.Epsilon);
            Assert.Equal("laplace", result.DifferentialPrivacySettings.Mechanism);
        }

        [Fact]
        public void Parse_DifferentialPrivacySettings_CustomValues_ParsedCorrectly()
        {
            var json = "{\"differentialPrivacySettings\": {\"epsilon\": 0.5, \"mechanism\": \"gaussian\"}}";
            var result = ParameterConfigurationParser.ParseFromJson(json);
            Assert.NotNull(result.DifferentialPrivacySettings);
            Assert.Equal(0.5, result.DifferentialPrivacySettings.Epsilon);
            Assert.Equal("gaussian", result.DifferentialPrivacySettings.Mechanism);
        }

        [Fact]
        public void ParseFromJson_DuplicatePropertyNames_ThrowsAnonymizerConfigurationException()
        {
            // ParseFromJson uses DuplicatePropertyNameHandling.Error — duplicate keys must throw.
            var json = "{\"dateShiftKey\": \"key1\", \"dateShiftKey\": \"key2\"}";
            Assert.Throws<AnonymizerConfigurationException>(() =>
                ParameterConfigurationParser.ParseFromJson(json));
        }

        [Fact]
        public void ParseFromAnonymizerConfigJson_MissingParametersBlock_ReturnsNullConfig()
        {
            var json = "{\"fhirVersion\": \"R4\", \"processingErrors\": \"skip\"}";
            var result = ParameterConfigurationParser.ParseFromAnonymizerConfigJson(json);
            Assert.Null(result);
        }

        [Fact]
        public void ParseFromAnonymizerConfigJson_WithParametersBlock_ParsesParameters()
        {
            var json = "{\"fhirVersion\": \"R4\", \"parameters\": {\"dateShiftKey\": \"testKey\", \"enablePartialAgesForRedact\": true}}";
            var result = ParameterConfigurationParser.ParseFromAnonymizerConfigJson(json);
            Assert.NotNull(result);
            Assert.Equal("testKey", result.DateShiftKey);
            Assert.True(result.EnablePartialAgesForRedact);
        }

        [Fact]
        public void Parse_InvalidDateShiftScope_ThrowsAnonymizerConfigurationException()
        {
            var json = "{\"dateShiftScope\": \"invalid_scope\"}";
            Assert.Throws<AnonymizerConfigurationException>(() =>
                ParameterConfigurationParser.ParseFromJson(json));
        }

        // ── Fail-secure type-guard tests ────────────────────────────────────────────

        [Fact]
        public void Parse_KAnonymitySettings_NonObjectToken_ThrowsAnonymizerConfigurationException()
        {
            var json = "{\"kAnonymitySettings\": \"not-an-object\"}";
            Assert.Throws<AnonymizerConfigurationException>(() =>
                ParameterConfigurationParser.ParseFromJson(json));
        }

        [Fact]
        public void Parse_DifferentialPrivacySettings_NonObjectToken_ThrowsAnonymizerConfigurationException()
        {
            var json = "{\"differentialPrivacySettings\": 42}";
            Assert.Throws<AnonymizerConfigurationException>(() =>
                ParameterConfigurationParser.ParseFromJson(json));
        }

        [Fact]
        public void Parse_RestrictedZipCodes_NonArrayToken_ThrowsAnonymizerConfigurationException()
        {
            var json = "{\"restrictedZipCodeTabulationAreas\": \"not-an-array\"}";
            Assert.Throws<AnonymizerConfigurationException>(() =>
                ParameterConfigurationParser.ParseFromJson(json));
        }

        // ── Domain validation tests ─────────────────────────────────────────────────

        [Fact]
        public void Parse_KAnonymityKValue_LessThanTwo_ThrowsAnonymizerConfigurationException()
        {
            var json = "{\"kAnonymitySettings\": {\"kValue\": 1}}";
            Assert.Throws<AnonymizerConfigurationException>(() =>
                ParameterConfigurationParser.ParseFromJson(json));
        }

        [Fact]
        public void Parse_SuppressionThreshold_OutOfRange_ThrowsAnonymizerConfigurationException()
        {
            var json = "{\"kAnonymitySettings\": {\"suppressionThreshold\": 1.5}}";
            Assert.Throws<AnonymizerConfigurationException>(() =>
                ParameterConfigurationParser.ParseFromJson(json));
        }

        [Fact]
        public void Parse_DifferentialPrivacyEpsilon_ZeroOrNegative_ThrowsAnonymizerConfigurationException()
        {
            var json = "{\"differentialPrivacySettings\": {\"epsilon\": 0}}";
            Assert.Throws<AnonymizerConfigurationException>(() =>
                ParameterConfigurationParser.ParseFromJson(json));
        }

        [Fact]
        public void Parse_DifferentialPrivacyDelta_OutOfRange_ThrowsAnonymizerConfigurationException()
        {
            // Use a valid epsilon so epsilon validation is not the cause of the throw.
            var json = "{\"differentialPrivacySettings\": {\"epsilon\": 1.0, \"delta\": 2.0}}";
            Assert.Throws<AnonymizerConfigurationException>(() =>
                ParameterConfigurationParser.ParseFromJson(json));
        }

        // ── New tests: null/non-object parameters token ──────────────────────────────

        [Fact]
        public void ParseFromAnonymizerConfigJson_NullParametersToken_DoesNotThrow()
        {
            // "parameters": null should be treated the same as an absent parameters key.
            var json = "{\"fhirVersion\": \"R4\", \"parameters\": null}";
            var result = ParameterConfigurationParser.ParseFromAnonymizerConfigJson(json);
            Assert.Null(result);
        }

        [Fact]
        public void ParseFromAnonymizerConfigJson_NonObjectParametersToken_ThrowsAnonymizerConfigurationException()
        {
            // "parameters" present as a string must throw — fail-secure.
            var json = "{\"fhirVersion\": \"R4\", \"parameters\": \"notanobject\"}";
            Assert.Throws<AnonymizerConfigurationException>(() =>
                ParameterConfigurationParser.ParseFromAnonymizerConfigJson(json));
        }

        // ── New tests: quasiIdentifiers / generalizationHierarchies type guards ──────

        [Fact]
        public void ParseKAnonymitySettings_InvalidQuasiIdentifiersType_ThrowsAnonymizerConfigurationException()
        {
            // quasiIdentifiers must be a JSON array; a string value must throw.
            var json = "{\"kAnonymitySettings\": {\"quasiIdentifiers\": \"not-an-array\"}}";
            Assert.Throws<AnonymizerConfigurationException>(() =>
                ParameterConfigurationParser.ParseFromJson(json));
        }

        [Fact]
        public void ParseKAnonymitySettings_InvalidGeneralizationHierarchiesType_ThrowsAnonymizerConfigurationException()
        {
            // generalizationHierarchies must be a JSON object; a string value must throw.
            var json = "{\"kAnonymitySettings\": {\"generalizationHierarchies\": \"not-an-object\"}}";
            Assert.Throws<AnonymizerConfigurationException>(() =>
                ParameterConfigurationParser.ParseFromJson(json));
        }

        // ── New tests: differential privacy sensitivity validation ───────────────────

        [Fact]
        public void ParseDifferentialPrivacySettings_ZeroSensitivity_ThrowsAnonymizerConfigurationException()
        {
            // sensitivity = 0 must throw; sensitivity must be strictly > 0.
            var json = "{\"differentialPrivacySettings\": {\"epsilon\": 1.0, \"sensitivity\": 0}}";
            Assert.Throws<AnonymizerConfigurationException>(() =>
                ParameterConfigurationParser.ParseFromJson(json));
        }

        [Fact]
        public void ParseDifferentialPrivacySettings_NegativeSensitivity_ThrowsAnonymizerConfigurationException()
        {
            // sensitivity < 0 must throw; sensitivity must be strictly > 0.
            var json = "{\"differentialPrivacySettings\": {\"epsilon\": 1.0, \"sensitivity\": -1.5}}";
            Assert.Throws<AnonymizerConfigurationException>(() =>
                ParameterConfigurationParser.ParseFromJson(json));
        }

        // ── EncryptKey byte-length validation tests ──────────────────────────────────

        [Fact]
        public void Parse_EncryptKey_16Bytes_Accepted()
        {
            // 16 ASCII characters = 16 UTF-8 bytes — valid AES-128 key size.
            var json = "{\"encryptKey\": \"abcdefghijklmnop\"}";
            var result = ParameterConfigurationParser.ParseFromJson(json);
            Assert.Equal("abcdefghijklmnop", result.EncryptKey);
        }

        [Fact]
        public void Parse_EncryptKey_24Bytes_Accepted()
        {
            // 24 ASCII characters = 24 UTF-8 bytes — valid AES-192 key size.
            var json = "{\"encryptKey\": \"abcdefghijklmnopqrstuvwx\"}";
            var result = ParameterConfigurationParser.ParseFromJson(json);
            Assert.Equal("abcdefghijklmnopqrstuvwx", result.EncryptKey);
        }

        [Fact]
        public void Parse_EncryptKey_32Bytes_Accepted()
        {
            // 32 ASCII characters = 32 UTF-8 bytes — valid AES-256 key size.
            var json = "{\"encryptKey\": \"abcdefghijklmnopqrstuvwxyz012345\"}";
            var result = ParameterConfigurationParser.ParseFromJson(json);
            Assert.Equal("abcdefghijklmnopqrstuvwxyz012345", result.EncryptKey);
        }

        [Fact]
        public void Parse_EncryptKey_InvalidLength_ThrowsAnonymizerConfigurationException()
        {
            // 15 ASCII characters = 15 UTF-8 bytes — not a valid AES key size.
            var json = "{\"encryptKey\": \"abcdefghijklmno\"}";
            Assert.Throws<AnonymizerConfigurationException>(() =>
                ParameterConfigurationParser.ParseFromJson(json));
        }

        [Fact]
        public void Parse_EncryptKey_NonAsciiExceedsBytes_ThrowsAnonymizerConfigurationException()
        {
            // Build a key that is 16 characters but exceeds 16 UTF-8 bytes.
            // 'e-acute' (U+00E9) encodes to 2 UTF-8 bytes; 14 ASCII chars + 2 x U+00E9 = 18 bytes.
            // Verifies the parser uses UTF-8 byte count rather than character count.
            var key = new string('a', 14) + "\u00e9\u00e9";
            var json = "{\"encryptKey\": \"" + key + "\"}";
            Assert.Throws<AnonymizerConfigurationException>(() =>
                ParameterConfigurationParser.ParseFromJson(json));
        }
    }
}
