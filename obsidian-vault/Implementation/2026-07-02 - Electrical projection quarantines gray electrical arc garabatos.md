# 2026-07-02 - Electrical projection quarantines gray electrical arc garabatos

type: Implementation
replaces: [[2026-07-02 - Electrical projection omits wiring and wall route garabatos]]
replaced_by: [[2026-07-02 - Electrical projection quarantines CFANLT block arc garabatos]]## What
Dependent ElectricalPlan DXF export now also quarantines `ARC`/`SPLINE` entities on layer `ELECTRICAL` when they have explicit DXF color `253`.

## Why
TEST 8 proved the prior rule removed `ELECTRICAL WIRING` and `ELECTRICAL WALLS` curves, but the user-visible remaining garabatos were still present. The latest DXF evidence identified the remaining visible curves as 8 gray `ARC` entities on layer `ELECTRICAL`, color `253`, with handles such as `129C`, `129D`, `12A5`, `12B7`, `12A8`, `12AD`, `12B1`, and `12B6`.

## Where
- `src/FloorplanFit.Infrastructure/Dxf/ProjectedPlanSheetDxfExporter.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Dxf/ProjectedPlanSheetDxfExporterTests.cs`

## Evidence
Applying the updated policy to the current TEST 8 plan-set would quarantine exactly 8 `ELECTRICAL`/`253` ARC entities. The largest remaining arcs after that are small symbols/fixtures with radii about 2.3 or less, not the visible gray garabatos.

## Caveat
Existing exported files like TEST 8 are immutable. The user must export a new package after recompilation (for example TEST 9) to see the change in AutoCAD.