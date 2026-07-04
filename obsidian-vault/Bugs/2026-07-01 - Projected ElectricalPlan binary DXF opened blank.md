---
type: bug
status: fixed
created: 2026-07-01
replaces:
replaced_by:
---

# Projected ElectricalPlan binary DXF opened blank

## What happened
After the HousePlanSet package export finally produced a folder/manifest, the exported dependent ElectricalPlan DXF opened in CAD as a black/empty `Drawing1` tab.

## Root cause
`ProjectedPlanSheetDxfExporter` assumed every dependent DXF was text DXF group-code pairs. The SEMINOLE electrical sheet is an AutoCAD binary DXF, so reading it with `File.ReadAllLines(...)` and writing it back as text corrupted the file structure.

## Fix
- Detect `AutoCAD Binary DXF` sources before the text-pair projection path.
- For binary DXFs, load with IxMilia, project common model entities, and save a valid text DXF.
- Keep the existing text-DXF path unchanged so canonical/floorplan export behavior is not touched.
- Added a regression test proving a binary DXF can be projected and reloaded by IxMilia.

## Files
- `src/FloorplanFit.Infrastructure/Dxf/ProjectedPlanSheetDxfExporter.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Dxf/ProjectedPlanSheetDxfExporterTests.cs`

## Verification
- Static whitespace check passed for touched files.
- `rg` verified the binary branch and regression test are present.
- No build/test run by agent because this repository forbids building after changes.

## Manual retest
Restart `dotnet watch`, export the HousePlanSet package again, then open the newly generated ElectricalPlan DXF inside the new `*-plan-set` folder.

## Follow-up compile fix
`DxfArc` inherits from `DxfCircle` in IxMilia, so the binary projection switch must match `DxfArc` before `DxfCircle`. The case order was corrected after `dotnet watch` reported CS8120 unreachable switch case.

## Follow-up invalid AutoCAD fix
Runtime smoke showed the new package file was not the old binary file; it was a newly generated text DXF that AutoCAD still rejected. The binary branch no longer uses `DxfFile.Save(...)`. It now parses AutoCAD binary DXF group-code pairs, preserves all source sections/objects/binary chunks, applies the existing coordinate projection to those pairs, and writes source-preserving text DXF. This mirrors the floorplan lesson: do not reserialize real CAD files through a lossy library path when AutoCAD validity matters.

## Follow-up binary-preserving export
`TEST4` proved the source-preserving text DXF had all sections and entities and passed `ezdxf` recovery, but AutoCAD still opened it incorrectly. The dependent binary branch now writes AutoCAD Binary DXF back out instead of converting to text. This keeps the original binary container and only changes projected group-code values.

## Follow-up section-scoped projection
`TEST5` proved the latest export was a binary DXF and `ezdxf` could read it, but it still opened incorrectly in AutoCAD. Byte inspection showed the exporter had projected group-code coordinates globally, including `HEADER`, `TABLES`, `BLOCKS`, and `OBJECTS`. The exporter now projects only the `ENTITIES` section and leaves metadata/header/object sections untouched. Added regression coverage that `$EXTMIN/$EXTMAX` remain unchanged while model entities are projected.

## Runtime smoke confirmation
`TEST6-plan-set` was generated after scoping projection to `ENTITIES` only. The user reported that this export now opens/appears correctly. This confirms the prior failures were caused by projecting DXF metadata/header/object coordinates, not by the binary container alone or missing model geometry.

## Follow-up visible wiring arcs/garabato
User compared original vs projected ElectricalPlan and showed that large curved wiring lines become visibly prominent in the projected file even though they are not visually apparent in the original. Inspection found large `ARC` entities on `ELECTRICAL WIRING` with the same handles in source and output, but the current projection still transforms wiring-route geometry. Design implication: dependent projection needs a layer/entity policy. Freeform wiring routes (`ELECTRICAL WIRING`, large arcs, splines) should not be blindly projected/deformed like walls/symbols; they need quarantine/manual review or an explicit routing strategy.


## 2026-07-01 - Verified TEST6 ElectricalPlan entity-count parity

Comparison:
- Source: `D:\PointAIData\PLANS\original electrical plans\ELECTRICAL PLAN SEMINOLE 2000.dxf`
- Output: `D:\PointAIData\PLANS\original electrical plans\TEST6-plan-set\ELECTRICAL PLAN SEMINOLE 2000-12-7cdf74bff7664c53969de447e676fafc.dxf`

Evidence:
- Modelspace entities: 3063 source, 3063 output.
- Entity counts match exactly for ARC/CIRCLE/SPLINE/LWPOLYLINE/ELLIPSE/LINE/INSERT.
- Suspicious layers match exactly: `ELECTRICAL WALLS` = 1000, `ELECTRICAL WIRING` = 75, `ELECTRICAL` = 832.
- ARC handles match exactly: 215 source, 215 output, 0 added, 0 missing.
- Large visible curves are existing ARC handles (for example `1537`, `1009`, `100B`, `FF8`, `FFA`) whose coordinates were projected, not newly created.

Conclusion:
The dependent exporter is not adding new circle/arc entities in TEST6. The real product bug is semantic: the current projector treats electrical wiring/wall route curves as normal geometry, so existing large curves can become visually prominent/misleading after projection. Next fix should be a layer/entity projection policy for dependent electrical sheets, not another broad DXF serializer change.
