---
type: Bugs
status: resolved
date: 2026-07-14
---

# Estimator rationale compile collision

## Symptom

`dotnet watch` could not build the Desktop app because `EvaluateCandidate` declared branch-local variables named `rationale` that collided with the method-scope `rationale` variable (`CS0136`).

## Fix

The branch-local variables are now named `scaleRejectionRationale`, `outlineOnlyRationale`, and `missingEvidenceRationale`. The warning in `PlanSetOutlineSegmentCongruenceAuditBuilder.cs` is non-fatal and remains separate from this build error.

## Verification

Static inspection confirms the three branch names are present and the conflicting declarations are gone. Runtime build must be rerun by the user from the exact repository launcher.

