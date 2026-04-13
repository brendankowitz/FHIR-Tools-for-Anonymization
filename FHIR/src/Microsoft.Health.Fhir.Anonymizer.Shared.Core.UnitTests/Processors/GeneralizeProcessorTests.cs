using System;
using System.Collections.Generic;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors
{
    public class GeneralizeProcessorTests
    {
        private readonly GeneralizeProcessor _processor = new GeneralizeProcessor();

        // Helper: build a valid settings dictionary with a JSON cases string
        private static Dictionary<string, object> BuildSettings(string casesJson, string otherValues = null)
        {
            var settings = new Dictionary<string, object>
            {
                { "cases", casesJson }
            };
            if (otherValues != null)
            {
                settings["otherValues"] = otherValues;
            }
            return settings;
        }

        // Helper: build a valid ProcessContext
        private static ProcessContext ValidContext() =>
            new ProcessContext { VisitedNodes = new HashSet<ElementNode>() };

        // -----------------------------------------------------------------------
        // (a) Null node → ArgumentNullException
        //     EnsureArg.IsNotNull(node) throws when node is null.
        // -----------------------------------------------------------------------

        [Fact]
        public void Process_NullNode_ThrowsArgumentNullException()
        {
            var settings = BuildSettings("{\"$this = '5'\": \"'replaced'\"}");
            Assert.Throws<ArgumentNullException>(() =>
                _processor.Process(null, ValidContext(), settings));
        }

        // -----------------------------------------------------------------------
        // (b) Null settings dictionary → ArgumentNullException
        //     EnsureArg.IsNotNull(settings) throws when settings is null.
        // -----------------------------------------------------------------------

        [Fact]
        public void Process_NullSettings_ThrowsArgumentNullException()
        {
            var node = ElementNode.FromElement(new FhirString("hello").ToTypedElement());
            Assert.Throws<ArgumentNullException>(() =>
                _processor.Process(node, ValidContext(), null));
        }

        // -----------------------------------------------------------------------
        // (c) Null context → ArgumentNullException
        //     EnsureArg.IsNotNull(context?.VisitedNodes) throws when context is null.
        // -----------------------------------------------------------------------

        [Fact]
        public void Process_NullContext_ThrowsArgumentNullException()
        {
            var node = ElementNode.FromElement(new FhirString("hello").ToTypedElement());
            var settings = BuildSettings("{\"$this = 'hello'\": \"'world'\"}");
            Assert.Throws<ArgumentNullException>(() =>
                _processor.Process(node, null, settings));
        }

        // -----------------------------------------------------------------------
        // (c2) Context with null VisitedNodes → ArgumentNullException
        //      EnsureArg.IsNotNull(context?.VisitedNodes) throws when VisitedNodes is null.
        // -----------------------------------------------------------------------

        [Fact]
        public void Process_ContextWithNullVisitedNodes_ThrowsArgumentNullException()
        {
            var node = ElementNode.FromElement(new FhirString("hello").ToTypedElement());
            var settings = BuildSettings("{\"$this = 'hello'\": \"'world'\"}");
            var context = new ProcessContext { VisitedNodes = null };
            Assert.Throws<ArgumentNullException>(() =>
                _processor.Process(node, context, settings));
        }

        // -----------------------------------------------------------------------
        // (d) Integer node matching first case → value replaced, IsGeneralized true
        // -----------------------------------------------------------------------

        [Fact]
        public void Process_IntegerNodeMatchingFirstCase_ValueReplacedAndIsGeneralizedTrue()
        {
            // Integer value 5 — case "$this <= 10" evaluates to true → replace with 'low'
            var node = ElementNode.FromElement(new Integer(5).ToTypedElement());
            var settings = BuildSettings("{\"$this <= 10\": \"'low'\", \"$this <= 20\": \"'mid'\"}");

            var result = _processor.Process(node, ValidContext(), settings);

            Assert.True(result.IsGeneralized);
            Assert.Equal("low", node.Value?.ToString());
        }

        // -----------------------------------------------------------------------
        // (e) Integer node matching no case, OtherValues = redact (default) → node.Value null
        // -----------------------------------------------------------------------

        [Fact]
        public void Process_IntegerNodeNoMatchWithRedactDefault_NodeValueBecomesNull()
        {
            // Integer value 5 — case "$this > 100" evaluates to false → no match
            // Default OtherValues is Redact → node.Value becomes null
            var node = ElementNode.FromElement(new Integer(5).ToTypedElement());
            var settings = BuildSettings("{\"$this > 100\": \"'high'\"}");

            var result = _processor.Process(node, ValidContext(), settings);

            Assert.Null(node.Value);
        }

        // -----------------------------------------------------------------------
        // (f) Integer node matching no case, OtherValues = keep → node.Value unchanged
        // -----------------------------------------------------------------------

        [Fact]
        public void Process_IntegerNodeNoMatchWithKeepOtherValues_NodeValueUnchanged()
        {
            // Integer value 5 — case "$this > 100" evaluates to false → no match
            // OtherValues = keep → node.Value stays as "5"
            var node = ElementNode.FromElement(new Integer(5).ToTypedElement());
            var originalValue = node.Value?.ToString();
            var settings = BuildSettings("{\"$this > 100\": \"'high'\"}", "keep");

            _processor.Process(node, ValidContext(), settings);

            Assert.Equal(originalValue, node.Value?.ToString());
        }

        // -----------------------------------------------------------------------
        // (g) String node with language-code cases → generalised correctly
        // -----------------------------------------------------------------------

        [Fact]
        public void Process_StringNodeWithLanguageCodeCase_GeneralisedCorrectly()
        {
            // String "en-US" — case "$this = 'en-US'" matches → replace with 'en'
            var node = ElementNode.FromElement(new FhirString("en-US").ToTypedElement());
            var settings = BuildSettings("{\"$this = 'en-US'\": \"'en'\", \"$this = 'fr-FR'\": \"'fr'\"}");

            var result = _processor.Process(node, ValidContext(), settings);

            Assert.True(result.IsGeneralized);
            Assert.Equal("en", node.Value?.ToString());
        }

        // -----------------------------------------------------------------------
        // (h) Non-primitive complex node → AnonymizerRuleNotApplicableException
        // -----------------------------------------------------------------------

        [Fact]
        public void Process_ComplexNonPrimitiveNode_ThrowsAnonymizerRuleNotApplicableException()
        {
            // HumanName is a complex (non-primitive) FHIR type
            var humanName = new HumanName { Family = "Smith" };
            var node = ElementNode.FromElement(humanName.ToTypedElement());
            var settings = BuildSettings("{\"$this.family = 'Smith'\": \"'anonymized'\"}");

            Assert.Throws<AnonymizerRuleNotApplicableException>(() =>
                _processor.Process(node, ValidContext(), settings));
        }

        // -----------------------------------------------------------------------
        // (i) Node with null value → returns empty ProcessResult, node unchanged
        //     The processor returns early when node.Value is null (no Generalize record).
        // -----------------------------------------------------------------------

        [Fact]
        public void Process_NodeWithNullValue_ReturnsEmptyProcessResult()
        {
            // FhirString with null value → ElementNode.Value will be null
            var node = ElementNode.FromElement(new FhirString(null).ToTypedElement());
            var settings = BuildSettings("{\"$this = 'something'\": \"'replaced'\"}");

            var result = _processor.Process(node, ValidContext(), settings);

            // No Generalize operation was recorded → IsGeneralized is false
            Assert.False(result.IsGeneralized);
            // Node value remains null
            Assert.Null(node.Value);
        }
    }
}
