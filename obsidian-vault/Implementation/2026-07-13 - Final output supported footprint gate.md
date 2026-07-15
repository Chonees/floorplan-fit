# Final output supported footprint gate

## What
Final output FloorPlan/ElectricalPlan congruence now gates on the supported structural footprint/huella instead of raw segment extents, and the UI/audit exposes raw visible-bounds mismatch separately.

## Why
TEST A showed raw FloorPlan `WALLS` bounds were inflated by low-support tail geometry. Using raw bounds as the automatic gate mixed references and produced false mismatch against ElectricalPlan.

## Where
- `src/FloorplanFit.Infrastructure/Storage/PlanSetOutlineSegmentCongruenceAuditBuilder.cs`
- `src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs`
- `scripts/verify-latest-plan-set-final-output-congruence.ps1`
- `scripts/verify-latest-plan-set-recipe-manifest.ps1`

## Also fixed
`IxMiliaAdjustedSitePlanExporter` now compresses HATCH 10/20 boundary points so FloorPlan visible hatch geometry does not stay in stale source coordinates.

## Evidence
Repo-allowed checks passed:
- `scripts/test-floorplan-exporter-hatch-compression-contract.ps1`
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1`
- `scripts/test-plan-set-human-summary-contract.ps1`
- `scripts/test-plan-set-observability-audit-files.ps1`
- `scripts/test-outline-segment-congruence-contract.ps1`
- `scripts/test-outline-congruence-contract.ps1`
- `git diff --check`

Runtime proof completed with manifest `b4e8192e70a146f3910a4fc77988b028`: FloorPlan and ElectricalPlan supported footprints both measure `464.40 x 930.00`, with `0.00` width/height mismatch. See [[2026-07-13 - Fresh final output congruence proof passed]].
