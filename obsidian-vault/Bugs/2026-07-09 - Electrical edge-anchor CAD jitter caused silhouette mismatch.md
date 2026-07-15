# 2026-07-09 - Electrical edge-anchor CAD jitter caused silhouette mismatch

## Status
Fixed in code; fresh Desktop re-export pending.

## What happened
The fresh `763c5f00...` export passed the operation verifier, but visual comparison still looked off.

Runtime geometry check showed why:
- Raw Electrical `ELECTRICAL WALLS` bbox height: `930.000286"`.
- Exported Electrical `ELECTRICAL WALLS` bbox height: `927.8"`.
- Expected local shrink was `2.4"`, so expected height is about `927.600286"`.

The top edge had CAD jitter: one wall line was at `Y 961.576429` while the registered max edge was `Y 961.576716`. With strict `>=` comparison, that near-edge line received only one of the two top compressions.

## Fix
Use a tiny `0.01"` coordinate tolerance when applying/auditing recipe pinch comparisons.

## Evidence
Read-only simulation from raw Electrical geometry:
- strict tolerance `0.0` -> height `927.8"`.
- CAD tolerance `0.01` -> height `927.600286"`.

## Files
- `src/FloorplanFit.Application/PlanSets/Projection/ElectricalRecipeProjection.cs`
- `src/FloorplanFit.Infrastructure/Dxf/ProjectedPlanSheetDxfExporter.cs`
- `tests/FloorplanFit.Application.Tests/PlanSets/Projection/ElectricalRecipeProjectionTests.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Dxf/ProjectedPlanSheetDxfExporterTests.cs`
- `scripts/test-electrical-edge-anchor-contract.ps1`

