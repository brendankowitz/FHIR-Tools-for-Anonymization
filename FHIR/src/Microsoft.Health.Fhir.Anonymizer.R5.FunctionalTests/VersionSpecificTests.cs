using System.IO;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.FunctionalTests
{
    public class VersionSpecificTests
    {
        [Theory]
        [InlineData("Subscription-example.json")]
        [InlineData("SubscriptionTopic-example.json")]
        [InlineData("ActorDefinition-example.json")]
        [InlineData("Requirements-example.json")]
        [InlineData("ArtifactAssessment-example.json")]
        public void GivenR5OnlyResources_WhenAnonymizing_AnonymizedJsonShouldBeReturned(string testFile)
        {
            // Arrange
            var testFilePath = Path.Combine("TestResources", "R5OnlyResource", testFile);
            var content = File.ReadAllText(testFilePath);
            var settings = new AnonymizerSettings()
            {
                IsPrettyOutput = false
            };
            var engine = new AnonymizerEngine("r5-configuration-sample.json", settings);

            // Act
            var result = engine.AnonymizeJson(content);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void GivenR5Configuration_WhenValidatingVersion_ShouldSucceed()
        {
            // Arrange
            var settings = new AnonymizerSettings()
            {
                IsPrettyOutput = false
            };

            // Act & Assert - should not throw
            var engine = new AnonymizerEngine("r5-configuration-sample.json", settings);
            Assert.NotNull(engine);
        }

        [Fact]
        public void GivenR5Patient_WhenAnonymizing_ShouldHandleR5SpecificElements()
        {
            // Arrange
            var patientJson = @"{
                ""resourceType"": ""Patient"",
                ""id"": ""example"",
                ""name"": [{
                    ""use"": ""official"",
                    ""family"": ""Doe"",
                    ""given"": [""John""]
                }],
                ""birthDate"": ""1974-12-25""
            }";
            var settings = new AnonymizerSettings()
            {
                IsPrettyOutput = false
            };
            var engine = new AnonymizerEngine("r5-configuration-sample.json", settings);

            // Act
            var result = engine.AnonymizeJson(patientJson);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.DoesNotContain("Doe", result);
            Assert.DoesNotContain("John", result);
        }
    }
}
