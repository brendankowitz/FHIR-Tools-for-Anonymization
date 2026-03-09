#!/usr/bin/env bash
# check-todo-comments.sh
#
# Scans security-sensitive source paths for TODO/FIXME developer annotations
# in C# single-line comments (// TODO or // FIXME).
#
# Exits 0 if no violations are found, non-zero otherwise.
#
# Only matches C# single-line comment style (//) to avoid false positives from
# string-literal occurrences (e.g. validation arrays in ParameterConfiguration.cs).

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Security-sensitive paths to scan
SENSITIVE_PATHS=(
  "FHIR/src/Microsoft.Health.Fhir.Anonymizer.Shared.Core/AnonymizerConfigurations"
  "FHIR/src/Microsoft.Health.Fhir.Anonymizer.Shared.Core/Processors"
  "FHIR/src/Microsoft.Health.Fhir.Anonymizer.Shared.Core/Visitors"
  "DICOM/src/Microsoft.Health.Dicom.Anonymizer.Core"
)

# Build the list of existing paths to scan
PATHS_TO_SCAN=()
for rel_path in "${SENSITIVE_PATHS[@]}"; do
  abs_path="$REPO_ROOT/$rel_path"
  if [ -d "$abs_path" ]; then
    PATHS_TO_SCAN+=("$abs_path")
  else
    echo "WARNING: Sensitive path not found (skipping): $rel_path" >&2
  fi
done

if [ ${#PATHS_TO_SCAN[@]} -eq 0 ]; then
  echo "ERROR: No sensitive paths found to scan. Check repository structure." >&2
  exit 2
fi

echo "Scanning security-sensitive paths for TODO/FIXME comments..."
echo "Paths scanned:"
for p in "${PATHS_TO_SCAN[@]}"; do
  echo "  - $p"
done
echo ""

# Grep for // TODO or // FIXME (single-line C# comment style only)
# -r: recursive, -n: line numbers, -i: case-insensitive for TODO/FIXME
VIOLATIONS=$(grep -rn --include="*.cs" -i '//[[:space:]]*\(TODO\|FIXME\)' "${PATHS_TO_SCAN[@]}" 2>/dev/null || true)

if [ -z "$VIOLATIONS" ]; then
  echo "✅ No TODO/FIXME comments found in security-sensitive paths."
  exit 0
fi

VIOLATION_COUNT=$(echo "$VIOLATIONS" | wc -l | tr -d ' ')

echo "❌ Found $VIOLATION_COUNT TODO/FIXME comment(s) in security-sensitive paths:"
echo ""
echo "$VIOLATIONS"
echo ""
echo "Policy: Unresolved TODO/FIXME annotations are not permitted in anonymization"
echo "configuration and method-implementation code. Please resolve all annotations"
echo "before merging. See docs/FHIR-anonymization.md for guidance."
exit 1
