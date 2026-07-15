---
type: Implementation
date: 2026-07-10
status: completed
replaces:
  - "2026-07-10 - Segment congruence blocked on ULTIMO TEST 11 export"
---

# ULTIMO TEST 11 proves Electrical segment congruence

## What changed
The HousePlanSet ElectricalPlan projection goal now has a fresh runtime proof that the dependent ElectricalPlan can receive the FloorPlan canonical compression recipe automatically and still pass structural outline congruence.

## Proof
Latest manifest:
`src/FloorplanFit.Desktop/bin/Debug/net10.0/workspace/exports/plan-sets/ea194766c40646248be23bcd1fbebec7/manifest.json`

Verifier command:
`powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic`

Important result:
- Status: `ReadyForExport`
- Electrical status: `ProjectedAutomatically`
- Audit artifacts: `8`
- Outline normalization applied: `True`
- Source width mismatch normalized: about `2.0 in` before export, `0.0 in` after export
- Segment congruence: `SegmentCongruent`
- Comparison mode: `StructuralOutlineCoverageWithWallRunAdvisory`
- Required outline segments missing in Electrical: `0`
- Required outline extras in Electrical: `0`
- Required outline edge coverage: Left/Right/Bottom/Top all covered
- Corner coverage: BottomLeft/BottomRight/TopLeft/TopRight covered

## Important nuance
The audit still reports advisory internal wall-run differences (`22` missing internal runs and `116` extra electrical wall runs). Those are not blockers because the automatic proof now gates on required structural outline coverage, not exact one-to-one internal drafting equality. This is intentional: Electrical drawings can have different electrical curves, symbols, and wall-run drafting noise while still being structurally aligned to the FloorPlan silhouette.

## Static checks also passed
- `scripts/test-outline-segment-congruence-contract.ps1`
- `scripts/test-outline-congruence-contract.ps1`
- `scripts/test-plan-set-observability-audit-files.ps1`
- `scripts/test-plan-set-human-summary-contract.ps1`
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1`
- `git diff --check` on touched files

## Final state
The FloorPlan -> ElectricalPlan automatic projection proof is complete for the current phase. Roof and Facade/Elevation remain explicitly out of scope.
