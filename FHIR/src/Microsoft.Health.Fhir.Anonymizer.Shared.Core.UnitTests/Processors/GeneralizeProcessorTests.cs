using System;
using System.Collections.Generic;
using System.Text.Json;
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

        /// <summary>
        /// Builds a valid settings dictionary from a type-safe cases dictionary.
        /// The cases dictionary maps FHIRPath condition expressions to their replacement values.
        /// Using a <see cref="Dictionary{TKey,TValue}"/> avoids JSON-escaping fragility
        /// and makes call sites self-documenting.
        /// </summary>
        /// <param name="cases">A dictionary of FHIRPath condition → replacement value pairs.</param>
        /// <param name="otherValues">Optional OtherValues operation (e.g. "redact", "keep"). When null, the
        /// default from <c>GeneralizeSetting.CreateFromRuleSettings</c> (Redact) is used.</param>
        private static Dictionary<string, object> BuildSettings(Dictionary<string, string> cases, string otherValues = null)
        {
            var settings = new Dictionary<string, object>
            {
                { "cases", JsonSerializer.Serialize(cases) }
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
            var settings = BuildSettings(new Dictionary<string, string>
            {
                { "$this = '5'", "'replaced'" }
            });
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
            var settings = BuildSettings(new Dictionary<string, string>
            {
                { "$this = 'hello'", "'world'" }
            });
            Assert.Throws<ArgumentNullException>(() =>
                _processor.Process(node, null, settings));
        }

        // -----------------------------------------------------------------------
        // (c2) Context with null VisitedNodes → ArgumentNullException
        //      EnsureArg.IsNotNull(context?.VisitedNodes) is evaluated eagerly at the top
        //      of Process() (before any case matching or early-return logic), so the guard
        //      fires regardless of whether the FHIRPath case expression would match.
        // -----------------------------------------------------------------------

        [Fact]
        public void Process_ContextWithNullVisitedNodes_ThrowsArgumentNullException()
        {
            var node = ElementNode.FromElement(new FhirString("hello").ToTypedElement());
            var settings = BuildSettings(new Dictionary<string, string>
            {
                { "$this = 'hello'", "'world'" }
            });
            var context = new ProcessContext { VisitedNodes = null };
            Assert.Throws<ArgumentNullException>(() =>
                _processor.Process(node, context, settings));
        }

        // -----------------------------------------------------------------------
        // (d) Integer node matching first case → value replaced with first case result,
        //     IsGeneralized true. The second case ($this <= 20 → 'mid') must NOT be
        //     applied because cases are evaluated in order and the first match wins.
        // -----------------------------------------------------------------------

        [Fact]
        public void Process_IntegerNodeMatchingFirstCase_ValueReplacedAndIsGeneralizedTrue()
        {
            // Integer value 5 — case "$this <= 10" evaluates to true → replace with 'low'.
            // The second case "$this <= 20" is NOT evaluated because the first case already matched.
            var node = ElementNode.FromElement(new Integer(5).ToTypedElement());
            var settings = BuildSettings(new Dictionary<string, string>
            {
                { "$this <= 10", "'low'" },
                { "$this <= 20", "'mid'" }
            });

            var result = _processor.Process(node, ValidContext(), settings);

            Assert.True(result.IsGeneralized);
            // Confirm the FIRST case result ('low') was applied, not the second case ('mid').
            Assert.Equal("low", node.Value?.ToString());
        }

        [Fact]
        public void Process_IntegerNodeAtFirstCaseBoundary_ValueReplacedWithFirstCaseResult()
        {
            // Integer value 10 — exactly at the upper boundary of "$this <= 10" → replace with 'low'.
            // Confirms the first case is inclusive at its boundary.
            var node = ElementNode.FromElement(new Integer(10).ToTypedElement());
            var settings = BuildSettings(new Dictionary<string, string>
            {
                { "$this <= 10", "'low'" },
                { "$this <= 20", "'mid'" }
            });

            var result = _processor.Process(node, ValidContext(), settings);

            Assert.True(result.IsGeneralized);
            Assert.Equal("low", node.Value?.ToString());
        }

        [Fact]
        public void Process_IntegerNodeJustAboveFirstCaseBoundary_ValueReplacedWithSecondCaseResult()
        {
            // Integer value 11 — just above the boundary of "$this <= 10", so the first case
            // does NOT match. The second case "$this <= 20" matches → replace with 'mid'.
            var node = ElementNode.FromElement(new Integer(11).ToTypedElement());
            var settings = BuildSettings(new Dictionary<string, string>
            {
                { "$this <= 10", "'low'" },
                { "$this <= 20", "'mid'" }
            });

            var result = _processor.Process(node, ValidContext(), settings);

            Assert.True(result.IsGeneralized);
            Assert.Equal("mid", node.Value?.ToString());
        }

        // -----------------------------------------------------------------------
        // (e) Integer node matching no case, OtherValues = redact (default) → node.Value null
        //
        // GeneralizeProcessor default OtherValues is GeneralizeOtherValuesOperation.Redact
        // (see GeneralizeSetting.CreateFromRuleSettings — when no 'otherValues' key is
        // present in the settings dictionary, it falls back to Redact).
        // -----------------------------------------------------------------------

        [Fact]
        public void Process_IntegerNodeNoMatchWithRedactDefault_NodeValueBecomesNull()
        {
            // Integer value 5 — case "$this > 100" evaluates to false → no match.
            // No 'otherValues' key in settings, so GeneralizeSetting defaults to Redact.
            // Default OtherValues is GeneralizeOtherValuesOperation.Redact → node.Value becomes null.
            var node = ElementNode.FromElement(new Integer(5).ToTypedElement());
            var settings = BuildSettings(new Dictionary<string, string>
            {
                { "$this > 100", "'high'" }
            });

            var result = _processor.Process(node, ValidContext(), settings);

            Assert.Null(node.Value);
        }

        [Fact]
        public void Process_IntegerNodeNoMatchWithExplicitRedact_NodeValueBecomesNull()
        {
            // Integer value 5 — case "$this > 100" evaluates to false → no match.
            // 'otherValues' is explicitly set to "redact" (not relying on the default).
            // This test isolates the 'explicit Redact sets value to null' behaviour from
            // the 'default is Redact' behaviour tested in Process_IntegerNodeNoMatchWithRedactDefault_NodeValueBecomesNull.
            var node = ElementNode.FromElement(new Integer(5).ToTypedElement());
            var settings = BuildSettings(
                new Dictionary<string, string> { { "$this > 100", "'high'" } },
                otherValues: "redact");

            var result = _processor.Process(node, ValidContext(), settings);

            Assert.Null(node.Value);
        }

        // -----------------------------------------------------------------------
        // (f) Integer node matching no case, OtherValues = keep → node.Value unchanged
        // -----------------------------------------------------------------------

        [Fact]
        public void Process_IntegerNodeNoMatchWithKeepOtherValues_NodeValueUnchanged()
        {
            // Integer value 5 — case "$this > 100" evaluates to false → no match.
            // OtherValues = keep → node.Value stays as "5".
            var node = ElementNode.FromElement(new Integer(5).ToTypedElement());
            var originalValue = node.Value?.ToString();
            var settings = BuildSettings(
                new Dictionary<string, string> { { "$this > 100", "'high'" } },
                otherValues: "keep");

            _processor.Process(node, ValidContext(), settings);

            Assert.Equal(originalValue, node.Value?.ToString());
        }

        // -----------------------------------------------------------------------
        // (g) String node with language-code cases → generalised correctly
        // -----------------------------------------------------------------------

        [Fact]
        public void Process_StringNodeWithLanguageCodeCase_GeneralisedCorrectly()
        {
            // String "en-US" — case "$this = 'en-US'" matches → replace with 'en'.
            var node = ElementNode.FromElement(new FhirString("en-US").ToTypedElement());
            var settings = BuildSettings(new Dictionary<string, string>
            {
                { "$this = 'en-US'", "'en'" },
                { "$this = 'fr-FR'", "'fr'" }
            });

            var result = _processor.Process(node, ValidContext(), settings);

            Assert.True(result.IsGeneralized);
            Assert.Equal("en", node.Value?.ToString());
        }

        // -----------------------------------------------------------------------
        // (h) Non-primitive complex node → AnonymizerRuleNotApplicableException
        //
        // HumanName is a complex FHIR BackboneElement (non-primitive type), so
        // GeneralizeProcessor must reject it. Any other complex FHIR type (e.g.
        // Address, ContactPoint) would behave the same way.
        // -----------------------------------------------------------------------

        [Fact]
        public void Process_ComplexNonPrimitiveNode_ThrowsAnonymizerRuleNotApplicableException()
        {
            // HumanName is a complex (non-primitive) FHIR BackboneElement (non-primitive type),
            // so GeneralizeProcessor must reject it. Any other complex FHIR type
            // (e.g. Address, ContactPoint) would behave the same way.
            var humanName = new HumanName { Family = "Smith" };
            var node = ElementNode.FromElement(humanName.ToTypedElement());
            var settings = BuildSettings(new Dictionary<string, string>
            {
                { "$this.family = 'Smith'", "'anonymized'" }
            });

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
            // FhirString with null value → ElementNode.Value will be null.
            var node = ElementNode.FromElement(new FhirString(null).ToTypedElement());
            var settings = BuildSettings(new Dictionary<string, string>
            {
                { "$this = 'something'", "'replaced'" }
            });

            var result = _processor.Process(node, ValidContext(), settings);

            // No Generalize operation was recorded → IsGeneralized is false.
            Assert.False(result.IsGeneralized);
            // Node value remains null.
            Assert.Null(node.Value);
        }

        // -----------------------------------------------------------------------
        // (j) Empty cases dictionary → no match, default Redact → node.Value null
        //     Confirms the processor handles no-case-defined configurations
        //     gracefully without throwing NullReferenceException or KeyNotFoundException.
        // -----------------------------------------------------------------------

        [Fact]
        public void Process_EmptyCasesDictionary_NoMatchAppliesDefaultOtherValues()
        {
            // Integer value 42 — zero cases defined, so there can never be a match.
            // Default OtherValues is Redact → node.Value must become null.
            var node = ElementNode.FromElement(new Integer(42).ToTypedElement());
            var settings = BuildSettings(new Dictionary<string, string>());

            // Must not throw NullReferenceException or KeyNotFoundException.
            var result = _processor.Process(node, ValidContext(), settings);

            // No case matched → default Redact applies → value is null.
            Assert.Null(node.Value);
        }

        // -----------------------------------------------------------------------
        // (k) Numeric boundary tests — zero and negative values
        // -----------------------------------------------------------------------

        [Fact]
        public void Process_IntegerNodeWithZeroValue_MatchingCase_ValueReplaced()
        {
            // Integer value 0 — case "$this = 0" exactly matches zero → replace with 'zero'.
            var node = ElementNode.FromElement(new Integer(0).ToTypedElement());
            var settings = BuildSettings(new Dictionary<string, string>
            {
                { "$this = 0", "'zero'" }
            });

            var result = _processor.Process(node, ValidContext(), settings);

            Assert.True(result.IsGeneralized);
            Assert.Equal("zero", node.Value?.ToString());
        }

        [Fact]
        public void Process_IntegerNodeWithNegativeValue_MatchingCase_ValueReplaced()
        {
            // Integer value -5 — case "$this < 0" matches negative values → replace with 'negative'.
            var node = ElementNode.FromElement(new Integer(-5).ToTypedElement());
            var settings = BuildSettings(new Dictionary<string, string>
            {
                { "$this < 0", "'negative'" }
            });

            var result = _processor.Process(node, ValidContext(), settings);

            Assert.True(result.IsGeneralized);
            Assert.Equal("negative", node.Value?.ToString());
        }

        [Fact]
        public void Process_IntegerNodeWithNegativeValue_NoMatch_RedactDefault()
        {
            // Integer value -5 — case "$this > 0" only matches positive values → no match.
            // Default OtherValues is Redact → node.Value becomes null.
            var node = ElementNode.FromElement(new Integer(-5).ToTypedElement());
            var settings = BuildSettings(new Dictionary<string, string>
            {
                { "$this > 0", "'positive'" }
            });

            var result = _processor.Process(node, ValidContext(), settings);

            Assert.Null(node.Value);
        }
    }
}
