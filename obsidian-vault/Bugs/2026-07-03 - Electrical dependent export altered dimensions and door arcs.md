---
type: bug
created: 2026-07-03
status: fixed
replaces:
replaced_by:
---

# Electrical dependent export altered dimensions and removed door arcs

## Symptom
After manual confirmation/re-export, the dependent ElectricalPlan preserved some source dimensions but they appeared displaced/odd, and door swing arcs disappeared because they were curved entities.

## Evidence
- The original SEMINOLE electrical DXF already had 17 `DIMENSION` modelspace entities; TEST9 also had 17, so dimensions were not invented.
- The exporter moved `DIMENSION` entities but did not project their anonymous dimension graphic blocks, which can make AutoCAD display dimension graphics out of alignment.
- The previous garabato quarantine removed every `ARC` on `ELECTRICAL WALLS`; door swings are also arcs on that layer, so valid doors were removed.

## Fix
- Removed destructive electrical-curve quarantine from dependent projection export.
- Added dimension block projection: anonymous blocks referenced by `DIMENSION` entities are projected with the dimension entity, while normal reusable blocks keep local geometry and only their inserts move.

## Files
- `src/FloorplanFit.Infrastructure/Dxf/ProjectedPlanSheetDxfExporter.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Dxf/ProjectedPlanSheetDxfExporterTests.cs`
