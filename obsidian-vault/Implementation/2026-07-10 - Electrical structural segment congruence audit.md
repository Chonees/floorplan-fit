# 2026-07-10 - Electrical structural segment congruence audit

## Status
Implemented in code; fresh Desktop export pending.

## Problem
`outline-congruence-audit.json` proves only bbox/envelope congruence. It does not prove that FloorPlan and Electrical structural wall segments actually match.

## Change
Added `outline-segment-congruence-audit.json` to the HousePlanSet audit package.

The audit compares exported FloorPlan vs exported Electrical structural segments:
- includes LINE, LWPOLYLINE, POLYLINE/VERTEX, SOLID, 3DFACE on WALL/EXTERIOR/STRUCT families;
- excludes Electrical layers from the canonical FloorPlan side;
- ignores text, dimensions, inserts, symbols, arcs/circles/splines by using a structural entity allow-list;
- reads both text and binary DXF;
- snaps to 0.05", drops small noise, normalizes by supported structural bounds, and compares long horizontal/vertical runs.

## Gate
`verify-latest-plan-set-recipe-manifest.ps1` now requires this artifact and fails `-RequireAutomatic` unless segment status is `SegmentCongruent`.

Allowed statuses:
- `SegmentCongruent`
- `SegmentMismatchRequiresRecipeRemap`
- `SegmentMismatchRequiresManualReview`
- `InsufficientData`

## Verification
Passed without dotnet build/test/watch:
- `scripts/test-outline-segment-congruence-contract.ps1`
- `scripts/test-outline-congruence-contract.ps1`
- `scripts/test-plan-set-observability-audit-files.ps1`
- `scripts/test-plan-set-human-summary-contract.ps1`
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1`
- `git diff --check` on touched files

Latest runtime verifier correctly fails on stale `ULTIMO TEST 8` because it predates `outline-segment-congruence-audit.json`.

## Next Step
Create a fresh Desktop export, e.g. `ULTIMO TEST 9`, then run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\verify-latest-plan-set-recipe-manifest.ps1" -RequireAutomatic
```
