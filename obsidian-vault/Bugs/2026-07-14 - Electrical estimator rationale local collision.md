---
type: Bugs
status: fixed
date: 2026-07-14
---

# Electrical estimator rationale local collision

## Symptom

Desktop failed to build with `CS0136` at estimator lines 568, 589 and 645 because three branch-local variables named `rationale` collided with the final method-scope `rationale` declaration.

## Fix

Renamed only the three branch variables to `scaleRejectionRationale`, `outlineOnlyRationale` and `missingEvidenceRationale`. Geometry, thresholds and candidate decisions were not changed.

## Verification

Static declaration inspection and untracked-file whitespace check passed. Per repository policy, no build was run after the change; the user's existing `dotnet watch` process is the external compile proof.

