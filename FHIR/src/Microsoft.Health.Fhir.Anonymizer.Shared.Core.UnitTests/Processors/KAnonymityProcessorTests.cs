using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors
{
    public class KAnonymityProcessorTests
    {
        [Fact]
        public void GivenValidKAnonymitySetting_WhenInitialize_ProcessorShouldBeCreated()
        {
            var processor = new KAnonymityProcessor();
            Assert.NotNull(processor);
        }

        [Fact]
        public void GivenMissingKValue_WhenProcess_ShouldThrowException()
        {
            var settings = new Dictionary<string, object>
            {
                { "quasiIdentifiers", new List<string> { "age", "gender" } }
            };

            var processor = new KAnonymityProcessor();
            var age = new Integer(45);
            var node = ElementNode.FromElement(age.ToTypedElement());
            
            Assert.Throws<ArgumentException>(() => processor.Process(node, null, settings));
        }

        [Fact]
        public void GivenInvalidKValue_WhenProcess_ShouldThrowException()
        {
            var settings = new Dictionary<string, object>
            {
                { "k", 1 },
                { "quasiIdentifiers", new List<string> { "age" } }
            };

            var processor = new KAnonymityProcessor();
            var age = new Integer(45);
            var node = ElementNode.FromElement(age.ToTypedElement());
            
            Assert.Throws<ArgumentException>(() => processor.Process(node, null, settings));
        }

        [Fact]
        public void GivenMissingQuasiIdentifiers_WhenProcess_ShouldThrowException()
        {
            var settings = new Dictionary<string, object>
            {
                { "k", 5 }
            };

            var processor = new KAnonymityProcessor();
            var age = new Integer(45);
            var node = ElementNode.FromElement(age.ToTypedElement());
            
            Assert.Throws<ArgumentException>(() => processor.Process(node, null, settings));
        }

        [Fact]
        public void GivenAgeNode_WhenProcessWithRangeGeneralization_ShouldGeneralizeToRange()
        {
            var settings = new Dictionary<string, object>
            {
                { "k", 3 },
                { "quasiIdentifiers", new List<string> { "age" } },
                { "generalizationStrategy", "range" },
                { "ageRangeSize", 10 }
            };

            var processor = new KAnonymityProcessor();
            
            // Test with age 45 -> should generalize to "40-49"
            var ageValue = new Integer(45);
            var node = ElementNode.FromElement(ageValue.ToTypedElement());
            var result = processor.Process(node, null, settings);

            // K-anonymity processing marks the node as k-anonymized
            Assert.True(result.IsKAnonymized);
            Assert.Equal("40-49", node.Value.ToString());
        }

        [Fact]
        public void GivenZipCodeNode_WhenProcessWithHierarchicalGeneralization_ShouldGeneralizeToPrefix()
        {
            var generalizationHierarchy = new Dictionary<string, object>
            {
                { "levels", new List<int> { 3, 2, 0 } } // Keep 3 digits, then 2, then suppress
            };

            var settings = new Dictionary<string, object>
            {
                { "k", 3 },
                { "quasiIdentifiers", new List<string> { "zipCode" } },
                { "generalizationStrategy", "hierarchy" },
                { "zipCodeHierarchy", generalizationHierarchy }
            };

            var processor = new KAnonymityProcessor();
            
            var zipCode = new FhirString("98052");
            var node = ElementNode.FromElement(zipCode.ToTypedElement());
            var result = processor.Process(node, null, settings);

            Assert.True(result.IsKAnonymized);
            Assert.StartsWith("980", node.Value.ToString());
        }

        [Fact]
        public void GivenGenderNode_WhenProcessWithSuppressionStrategy_ShouldMarkForSuppression()
        {
            var settings = new Dictionary<string, object>
            {
                { "k", 3 },
                { "quasiIdentifiers", new List<string> { "gender" } },
                { "generalizationStrategy", "suppression" }
            };

            var processor = new KAnonymityProcessor();
            
            var gender = new FhirString("male");
            var node = ElementNode.FromElement(gender.ToTypedElement());
            var result = processor.Process(node, null, settings);

            Assert.True(result.IsKAnonymized);
            Assert.Null(node.Value);
        }

        [Fact]
        public void GivenMultipleQuasiIdentifiers_WhenProcess_ShouldHandleAll()
        {
            var settings = new Dictionary<string, object>
            {
                { "k", 3 },
                { "quasiIdentifiers", new List<string> { "age", "gender", "zipCode" } },
                { "generalizationStrategy", "range" },
                { "ageRangeSize", 10 }
            };

            var processor = new KAnonymityProcessor();

            // Test age
            var age = new Integer(35);
            var ageNode = ElementNode.FromElement(age.ToTypedElement());
            var ageResult = processor.Process(ageNode, null, settings);
            Assert.True(ageResult.IsKAnonymized);

            // Test gender
            var gender = new FhirString("female");
            var genderNode = ElementNode.FromElement(gender.ToTypedElement());
            var genderResult = processor.Process(genderNode, null, settings);
            Assert.True(genderResult.IsKAnonymized);
        }

        [Fact]
        public void GivenEmptyNode_WhenProcess_ShouldReturnEmptyResult()
        {
            var settings = new Dictionary<string, object>
            {
                { "k", 3 },
                { "quasiIdentifiers", new List<string> { "age" } },
                { "generalizationStrategy", "range" }
            };

            var processor = new KAnonymityProcessor();
            
            var emptyAge = new Integer();
            var node = ElementNode.FromElement(emptyAge.ToTypedElement());
            var result = processor.Process(node, null, settings);

            // Empty nodes should still get processed result
            Assert.NotNull(result);
            Assert.True(result.IsKAnonymized);
        }

        [Fact]
        public void GivenKValue_WhenProcess_ShouldIncludeInPrivacyMetrics()
        {
            var settings = new Dictionary<string, object>
            {
                { "k", 5 },
                { "quasiIdentifiers", new List<string> { "age" } },
                { "generalizationStrategy", "range" }
            };

            var processor = new KAnonymityProcessor();
            var age = new Integer(25);
            var node = ElementNode.FromElement(age.ToTypedElement());
            var result = processor.Process(node, null, settings);

            Assert.True(result.PrivacyMetrics.ContainsKey("k-value"));
            Assert.Equal(5, result.PrivacyMetrics["k-value"]);
        }

        [Fact]
        public void GivenStrategyInSettings_WhenProcess_ShouldIncludeInPrivacyMetrics()
        {
            var settings = new Dictionary<string, object>
            {
                { "k", 3 },
                { "quasiIdentifiers", new List<string> { "age" } },
                { "generalizationStrategy", "range" }
            };

            var processor = new KAnonymityProcessor();
            var age = new Integer(42);
            var node = ElementNode.FromElement(age.ToTypedElement());
            var result = processor.Process(node, null, settings);

            Assert.True(result.PrivacyMetrics.ContainsKey("generalization-strategy"));
            Assert.Equal("range", result.PrivacyMetrics["generalization-strategy"].ToString());
        }
    }
}
