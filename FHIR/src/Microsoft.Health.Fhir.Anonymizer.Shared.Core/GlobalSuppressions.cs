// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// ---------------------------------------------------------------------------
// ParameterDefaults.cs defines string constants whose values are intentional
// sentinel/placeholder strings (e.g., a default key value that signals the
// caller must supply a real key).  The values happen to be the strings
// "TODO" / "FIXME" by design – they are NOT unresolved work items.
//
// SonarQube / SonarAnalyzer rules suppressed:
//   S1134 – Track uses of "FIXME" tags
//   S1135 – Track uses of "TODO" tags
// ---------------------------------------------------------------------------
[assembly: SuppressMessage(
    "SonarQube",
    "S1134:Track uses of FIXME tags",
    Justification = "String constant value 'FIXME' in ParameterDefaults is an intentional sentinel/placeholder, not an unresolved code comment.",
    Scope = "namespaceanddescendants",
    Target = "~N:Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations")]

[assembly: SuppressMessage(
    "SonarQube",
    "S1135:Track uses of TODO tags",
    Justification = "String constant value 'TODO' in ParameterDefaults is an intentional sentinel/placeholder, not an unresolved code comment.",
    Scope = "namespaceanddescendants",
    Target = "~N:Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations")]
