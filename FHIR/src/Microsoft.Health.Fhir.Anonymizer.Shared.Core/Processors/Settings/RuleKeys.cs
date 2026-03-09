namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings
{
    internal static class RuleKeys
    {
        internal const string ReplaceWith = "replaceWith";
        internal const string RangeType = "rangeType";
        internal const string RoundTo = "roundTo";
        internal const string Span = "span";

        internal const string Cases = "cases";
        internal const string OtherValues = "otherValues";

        internal const string KValue = "k";
        internal const string QuasiIdentifiers = "quasiIdentifiers";
        internal const string GeneralizationStrategy = "generalizationStrategy";
        internal const string GeneralizationHierarchy = "generalizationHierarchy";
        internal const string SuppressionStrategy = "suppressionStrategy";

        internal const string Epsilon = "epsilon";
        internal const string Delta = "delta";
        internal const string Sensitivity = "sensitivity";
        internal const string Mechanism = "mechanism";
    }
}
