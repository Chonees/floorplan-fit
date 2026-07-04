# 2026-07-02 - Electrical projection omits wiring route garabatos

type: Implementation
replaces:
replaced_by: [[2026-07-02 - Electrical projection omits wiring and wall route garabatos]]## What
Dependent ElectricalPlan DXF export now quarantines freeform wiring-route curves by omitting `ARC` and `SPLINE` entities on layer `ELECTRICAL WIRING` before applying the projection transform.

## Why
The SEMINOLE ElectricalPlan comparison showed no new ARC/CIRCLE entities were invented, but existing large route curves became visually dominant after projection. Removing these freeform route curves is the smallest safe step to eliminate the visible garabatos without touching canonical FloorPlan export or DXF metadata/header handling.

## Where
- `src/FloorplanFit.Infrastructure/Dxf/ProjectedPlanSheetDxfExporter.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Dxf/ProjectedPlanSheetDxfExporterTests.cs`

## Evidence
On `D:\PointAIData\PLANS\original electrical plans\ELECTRICAL PLAN SEMINOLE 2000.dxf`, this policy omits 74 route-curve entities: 28 `ARC` and 46 `SPLINE` on `ELECTRICAL WIRING`. Other electrical lines, walls, symbols, and metadata remain in the export path.

## Caveat
This is a quarantine, not final electrical rerouting. Proper wiring reroute should happen later through a dependent local-adjustment recipe/manual review module.