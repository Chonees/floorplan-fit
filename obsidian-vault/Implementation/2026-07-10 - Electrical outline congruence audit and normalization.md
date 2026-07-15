# 2026-07-10 - Electrical outline congruence audit and normalization

## Status
Implemented in code; fresh Desktop export pending.

## Problem
`ULTIMO TEST 4` proved recipe replay and DXF safety can pass while visual overlay is still wrong:
- Electrical raw structural width: `470"`.
- Canonical FloorPlan source width: `468"`.
- Same shrink `3.6"` made Electrical final width `466.4"`, not canonical final `464.4"`.

## Change
Electrical export now carries outline context:
- canonical source width/height from FloorPlan input audit,
- dependent Electrical structural bounds from generic wall/exterior/structural layer families,
- axis-specific outline normalization,
- outline congruence audit.

The Electrical projection pipeline is now:
1. Electrical source point.
2. Electrical -> FloorPlan registration.
3. Electrical outline normalization to canonical FloorPlan source outline.
4. Canonical shrink recipe.
5. Final output coordinate.

## Safety
- Normalization is axis-specific, not blind uniform scaling.
- If the source mismatch is over `12"` the exporter downgrades to manual review.
- Existing DXF safety and unsupported crossing guards still run after normalization.

## Observability
New audit artifact:
- `outline-congruence-audit.json`

Verifier now requires this artifact and prints:
- `OutlineCongruenceStatus`
- `OutlineNormalizationApplied`
- source width/height mismatch
- export width/height mismatch

## Verification
Passed without build/watch:
- `scripts/test-outline-congruence-contract.ps1`
- `scripts/test-plan-set-observability-audit-files.ps1`
- `scripts/test-plan-set-human-summary-contract.ps1`
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1`
- `git diff --check`

Latest runtime verifier correctly fails until a fresh export exists:
`f407546...` is missing `outline-congruence-audit.json`.


## 2026-07-10 follow-up
- Tightened outline bounds collection so normalization uses only structural outline geometry on WALL/EXTERIOR/STRUCT families, not every entity on those layers.
- If structural outline bounds are unavailable and the exporter can only see generic/all-entity bounds, `outline-congruence-audit.json` reports `InsufficientData` instead of a false `Congruent`.
- Allowed checks still pass; latest runtime manifest remains stale until a fresh Desktop export creates the new audit artifact.

## 2026-07-10 final runtime proof - ULTIMO TEST 8
- Fresh manifest: `bcab33898c764363b96f10fa20a3263f`.
- `verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic` passed.
- Status: `ReadyForExport`.
- Electrical status: `ProjectedAutomatically`.
- `outline-congruence-audit.json` exists and reports `MismatchRequiresNormalization` with `NormalizationApplied = true`.
- Source mismatch: width `2.0000000001162"`, height `0.0002864625128"`.
- Export mismatch after normalization and recipe: width `0.000000000000"`, height `0.000286462513"`.
- FloorPlan operations: `8/8 Applied`.
- Electrical operations: `8/8 Applied`.
- DXF safety: output exists, entity count before/after `3425`, missing handles `0`, missing owners `0`, unsupported crossings `0`.
- Human summary now explains the input shrink, FloorPlan impact, Electrical operations, outline normalization, and DXF safety.
