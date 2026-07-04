# 2026-07-02 - Electrical projection quarantines wall polyline ellipse garabatos

type: Implementation
replaces: [[2026-07-02 - Electrical projection quarantines CFANLT block arc garabatos]]
replaced_by:

## What
Dependent ElectricalPlan export now also quarantines `LWPOLYLINE` and `ELLIPSE` entities on layer `ELECTRICAL WALLS`, preserving ordinary `LINE` geometry.

## Why
The latest post-BLOCKS-fix export (`TEST-BLOCK-FIX-plan-set`) had zero remaining quarantined direct/block `ARC`/`SPLINE` entities, but visible rounded electrical-wall artifacts persisted. Audit showed the remaining candidates were 24 `LWPOLYLINE` and 4 `ELLIPSE` entities on `ELECTRICAL WALLS` color `251`.

## Where
- `src/FloorplanFit.Infrastructure/Dxf/ProjectedPlanSheetDxfExporter.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Dxf/ProjectedPlanSheetDxfExporterTests.cs`

## Evidence
On `TEST-BLOCK-FIX-plan-set`, updated policy would quarantine 28 direct entities:
- `ELECTRICAL WALLS` color `251` `LWPOLYLINE`: 24
- `ELECTRICAL WALLS` color `251` `ELLIPSE`: 4

It keeps `ELECTRICAL WALLS` ordinary wall lines: 832 `LINE` entities.

## Caveat
This is still a quarantine strategy for dependent electrical visual noise, not final semantically correct electrical rerouting.