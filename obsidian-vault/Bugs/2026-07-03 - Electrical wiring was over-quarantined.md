---
type: bug
created: 2026-07-03
status: fixed
replaces:
replaced_by:
---

# Electrical wiring curves were quarantined with garabato curves

## Symptom
After confirming/re-exporting a HousePlanSet ElectricalPlan, the exported electrical sheet kept dimension entities but lost the visible wiring/cable curves.

## Evidence
Compared `D:\PointAIData\PLANS\original electrical plans\ELECTRICAL PLAN SEMINOLE 2000.dxf` against the TEST9 plan-set export:

- Original `ELECTRICAL WIRING`: 46 SPLINE, 28 ARC, 1 DIMENSION.
- Old exporter filter removed all 46 SPLINE and 28 ARC from `ELECTRICAL WIRING`.
- Dimensions were not spawned: original modelspace had 17 DIMENSION entities and exported modelspace also had 17.

## Root cause
`ProjectedPlanSheetDxfExporter.OmitElectricalRouteCurves` over-quarantined `ELECTRICAL WIRING` arcs/splines while trying to suppress the large garabato candidates from `ELECTRICAL WALLS`.

## Fix
`ELECTRICAL WIRING` is no longer quarantined. The exporter still quarantines the known wall garabato candidates on `ELECTRICAL WALLS` and the color-253 `ELECTRICAL` arcs/splines.

## Files
- `src/FloorplanFit.Infrastructure/Dxf/ProjectedPlanSheetDxfExporter.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Dxf/ProjectedPlanSheetDxfExporterTests.cs`
