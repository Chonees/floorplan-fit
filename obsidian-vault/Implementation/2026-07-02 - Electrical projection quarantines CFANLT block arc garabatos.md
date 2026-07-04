# 2026-07-02 - Electrical projection quarantines CFANLT block arc garabatos

type: Implementation
replaces: [[2026-07-02 - Electrical projection quarantines gray electrical arc garabatos]]
replaced_by: [[2026-07-02 - Electrical projection quarantines wall polyline ellipse garabatos]]## What
The dependent ElectricalPlan exporter now applies the same quarantined electrical curve removal to both DXF `ENTITIES` and `BLOCKS` sections.

## Why
The latest export (`plano-ajustado-al-sitio-plan-set`) had zero quarantined modelspace entities left, but AutoCAD still displayed the curves because they came from block geometry. The specific source was the inserted block `CFANLT`, whose block definition contained 8 `ARC` entities on layer `ELECTRICAL`, color `253`.

## Where
- `src/FloorplanFit.Infrastructure/Dxf/ProjectedPlanSheetDxfExporter.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Dxf/ProjectedPlanSheetDxfExporterTests.cs`

## Evidence
`CFANLT` block quarantine candidates in the latest exported DXF:
- `28B`, `28C`, `294`, `2A6`: radius ~29.821
- `297`, `29C`, `2A0`, `2A5`: radius ~9.0

## Caveat
No re-registration is required. Existing generated DXF files remain unchanged; the user must export a new package after recompilation.