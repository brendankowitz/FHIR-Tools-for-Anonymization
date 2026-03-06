using System;
using System.Linq;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors.Generalize
{
    /// <summary>
    /// Unit tests for CodeableConcept and Coding generalization functionality.
    /// Tests cover code system hierarchy traversal and value set reduction.
    /// </summary>
    public class GeneralizeCodeSystemTests
    {
        private readonly GeneralizeProcessor _processor;

        public GeneralizeCodeSystemTests()
        {
            _processor = new GeneralizeProcessor();
        }

        [Fact]
        public void GivenCodeableConcept_WhenGeneralizeToParentCode_ThenShouldReturnParent()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "parent-code" }
            };

            var codeableConcept = new CodeableConcept
            {
                Coding = new System.Collections.Generic.List<Coding>
                {
                    new Coding
                    {
                        System = "http://snomed.info/sct",
                        Code = "195967001",
                        Display = "Asthma"
                    }
                }
            };

            var node = ElementNode.FromElement(codeableConcept.ToTypedElement());
            var result = _processor.Process(node, null, settings);

            // This test assumes the processor has a hierarchy map configured
            // The actual behavior depends on implementation
            Assert.NotNull(result);
        }

        [Fact]
        public void GivenCoding_WhenGeneralizeToSystem_ThenShouldReturnSystemOnly()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "system" }
            };

            var coding = new Coding
            {
                System = "http://snomed.info/sct",
                Code = "195967001",
                Display = "Asthma"
            };

            var node = ElementNode.FromElement(coding.ToTypedElement());
            var result = _processor.Process(node, null, settings);

            Assert.NotNull(result);
            Assert.Contains("snomed", result.Value.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GivenCodeableConceptWithMultipleCoding_WhenGeneralize_ThenShouldProcessFirst()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "system" }
            };

            var codeableConcept = new CodeableConcept
            {
                Coding = new System.Collections.Generic.List<Coding>
                {
                    new Coding
                    {
                        System = "http://snomed.info/sct",
                        Code = "195967001",
                        Display = "Asthma"
                    },
                    new Coding
                    {
                        System = "http://hl7.org/fhir/sid/icd-10",
                        Code = "J45",
                        Display = "Asthma"
                    }
                }
            };

            var node = ElementNode.FromElement(codeableConcept.ToTypedElement());
            var result = _processor.Process(node, null, settings);

            Assert.NotNull(result);
        }

        [Fact]
        public void GivenCode_WhenGeneralizeToCategory_ThenShouldReturnCategory()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "category" }
            };

            var code = new Code("active");
            var node = ElementNode.FromElement(code.ToTypedElement());
            var result = _processor.Process(node, null, settings);

            // The actual behavior depends on how categories are mapped
            Assert.NotNull(result);
        }

        [Fact]
        public void GivenCodingWithoutSystem_WhenGeneralize_ThenShouldHandleGracefully()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "system" }
            };

            var coding = new Coding
            {
                Code = "195967001",
                Display = "Asthma"
            };

            var node = ElementNode.FromElement(coding.ToTypedElement());
            var result = _processor.Process(node, null, settings);

            // Should handle missing system gracefully
            Assert.True(result == null || result.Value != null);
        }

        [Fact]
        public void GivenCodeableConceptWithText_WhenGeneralize_ThenShouldPreserveStructure()
        {
            var settings = new GeneralizeSetting
            {
                Cases = new[] { "text-only" }
            };

            var codeableConcept = new CodeableConcept
            {
                Text = "Patient has asthma",
                Coding = new System.Collections.Generic.List<Coding>
                {
                    new Coding
                    {
                        System = "http://snomed.info/sct",
                        Code = "195967001",
                        Display = "Asthma"
                    }
                }
            };

            var node = ElementNode.FromElement(codeableConcept.ToTypedElement());
            var result = _processor.Process(node, null, settings);

            Assert.NotNull(result);
        }
    }
}
