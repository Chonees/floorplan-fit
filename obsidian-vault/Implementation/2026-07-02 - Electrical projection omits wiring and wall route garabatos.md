# 2026-07-02 - Electrical projection omits wiring and wall route garabatos

type: Implementation
replaces: [[2026-07-02 - Electrical projection omits wiring route garabatos]]
replaced_by: [[2026-07-02 - Electrical projection quarantines gray electrical arc garabatos]]## What
Dependent ElectricalPlan DXF export now quarantines freeform route curves by omitting `ARC` and `SPLINE` entities on layers:
- `ELECTRICAL WIRING`
- `ELECTRICAL WALLS`

before applying the projection transform.

## Why
Runtime TEST7 proved the previous wiring-only fix worked technically (`ELECTRICAL WIRING` ARC/SPLINE dropped to 0), but the visible garabatos persisted because the remaining curves were `ELECTRICAL WALLS` arcs. The fix scope was too narrow.

## Where
- `src/FloorplanFit.Infrastructure/Dxf/ProjectedPlanSheetDxfExporter.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Dxf/ProjectedPlanSheetDxfExporterTests.cs`

## Evidence
On SEMINOLE original ElectricalPlan, the updated policy omits 201 route-curve entities:
- `ELECTRICAL WIRING`: 28 `ARC`, 46 `SPLINE`
- `ELECTRICAL WALLS`: 127 `ARC`

It keeps ordinary `ELECTRICAL WALLS` lines: 832 `LINE` entities.

## Caveat
This is still quarantine, not final electrical rerouting. Proper rerouting belongs in a later dependent local-adjustment/manual review module.