---
type: bug
created: 2026-07-03
status: fixed
replaces:
replaced_by:
---

# ElectricalPlan ellipse major axis was translated like a point

## Symptom
After dependent ElectricalPlan confirmation/re-export, small electrical wall ellipses appeared as huge compass/arrow-like arcs.

## Evidence
SEMINOLE original electrical DXF had 4 `ELLIPSE` entities on `ELECTRICAL WALLS`, color 251. Their original major/minor radii were about `9.26 / 6.28`. In the TEST 10 exported dependent sheet, the same ellipses became about `94 / 64` and `107 / 73`.

## Root cause
DXF `ELLIPSE` group codes `10/20` are the center point, but `11/21` are the major-axis vector. The exporter projected `11/21` with the same point transform as absolute coordinates, adding translation to a vector and inflating the radius.

## Fix
`ProjectedPlanSheetDxfExporter` now transforms `ELLIPSE` `11/21` with rotation/scale only, no translation. The center `10/20` still receives the full point transform.

## Files
- `src/FloorplanFit.Infrastructure/Dxf/ProjectedPlanSheetDxfExporter.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Dxf/ProjectedPlanSheetDxfExporterTests.cs`
