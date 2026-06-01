// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// ---------------------------------------------------------------------------
// The AnonymizerConfigurations test classes intentionally use the string
// literals "TODO" and "FIXME" as representative invalid-input values inside
// [InlineData] attributes.  These are test-data strings exercising validation
// logic for placeholder values – they are NOT unresolved work items.
//
// SonarQube / SonarAnalyzer rules suppressed:
//   S1134 – Track uses of "FIXME" tags
//   S1135 – Track uses of "TODO" tags
// ---------------------------------------------------------------------------
[assembly: SuppressMessage(
    "SonarQube",
    "S1134:Track uses of FIXME tags",
    Justification = "String literals 'FIXME' in [InlineData] attributes are intentional test data, not unresolved code comments.",
    Scope = "namespaceanddescendants",
    Target = "~N:Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations")]

[assembly: SuppressMessage(
    "SonarQube",
    "S1135:Track uses of TODO tags",
    Justification = "String literals 'TODO' in [InlineData] attributes are intentional test data, not unresolved code comments.",
    Scope = "namespaceanddescendants",
    Target = "~N:Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.AnonymizerConfigurations")]
