---
project: floorplan-fit
repo: https://github.com/Chonees/floorplan-fit
status: active
updated: 2026-05-15
loop: 4
area: dimensions-ordinate-reactive
---

# 2026-05-15 - Loop 4 native reactive ordinate projection

## What
Implemented the second real slice of Loop 4: resolved ordinate dimensions (`OrdinateX` / `OrdinateY`) now rebuild natively during reactive preview/export instead of staying authored/static behind safety guardrails.

## Why
The first Loop 4 slice made ordinates semantically correct and safe, but still static. That was the right first move, but it did not yet satisfy the product need that affected measurements should follow floor-plan movement. This slice closes that gap for native ordinate dimensions without lying and without degrading them to linears.

## Core behavior
- **Measurement truth** now comes from `datum -> feature` on the ordinate axis:
  - `OrdinateX` = `abs(feature.X - datum.X)`
  - `OrdinateY` = `abs(feature.Y - datum.Y)`
- **Visible ordinate geometry** follows the **feature delta**, not the datum delta:
  - leader endpoint moves with the feature
  - authored line primitives move with the feature
  - authored text/inserts/circles/arcs/solids move with the feature
- This preserves the intended semantic split:
  - datum changes affect the reported coordinate value
  - visible ordinate callout geometry stays attached to the measured feature

## Files
- `src/FloorplanFit.Application/FloorPlans/Review/ReactiveDimensionProjector.cs`
- `src/FloorplanFit.Application/FloorPlans/Review/DimensionGeometryProjector.cs`
- `tests/FloorplanFit.Application.Tests/FloorPlans/Review/ReactiveDimensionProjectorTests.cs`
- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/ExportAdjustedDxfHandlerTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/NativeDimensionPreviewControlTests.cs`

## Verification
- `dotnet test .\\tests\\FloorplanFit.Application.Tests\\FloorplanFit.Application.Tests.csproj --filter "ReactiveDimensionProjectorTests|ExportAdjustedDxfHandlerTests"`
- `dotnet test .\\tests\\FloorplanFit.Desktop.Tests\\FloorplanFit.Desktop.Tests.csproj --filter "NativeDimensionPreviewControlTests"`

## Notes
- Radius / Diameter are still outside this slice and remain pending for Loop 5.
- This slice closes reactive ordinate preview/export, but does **not** claim that ordinate-specific manual edit semantics are fully productized yet.
