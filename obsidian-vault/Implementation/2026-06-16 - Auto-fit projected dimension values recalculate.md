# 2026-06-16 - Auto-fit projected dimension values recalculate

## Type
Bugfix

## Current truth
- Applying an Adjust-to-Site-Plan recorte now recalculates bound dimension values when the selected pinch group affects the dimension interval.
- This works when the adjustment preview is in projected/site-plan coordinates while articulation bands remain in source/floor-plan coordinates.

## Root cause
- `DimensionIntervalReactiveProjector` decided whether to rebuild a dimension by comparing authored/projected anchor coordinates against the selected `ArticulationBandDto`.
- In real Adjust-to-Site-Plan, dimensions/geometries are projected into site-plan coordinates, but articulation bands originate from the published floor-plan curation/source coordinates.
- Because those coordinate spaces did not overlap, affected dimensions were translated but not rebuilt, so their displayed values stayed old.

## Fix
- Selected-band detection now accepts either:
  - the curated `DimensionIntervalBindingDto.IntervalStartCoordinate` / `IntervalEndCoordinate` overlapping the source articulation band, or
  - the authored/projected anchor interval overlapping a projected articulation band.
- This keeps existing projected-band behavior while fixing source-band behavior in the real preview flow.

## Product scope
- Loop 2: Adjust to Site Plan.
- Architecture layer: Application, because this is deterministic dimension projection logic independent from Desktop UI.

## Files changed
- `src/FloorplanFit.Application/FloorPlans/Review/DimensionIntervalReactiveProjector.cs`
- `tests/FloorplanFit.Application.Tests/FloorPlans/Review/DimensionIntervalReactiveProjectorTests.cs`

## Verification
- RED:
  - `dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --filter "FullyQualifiedName~Project_uses_binding_interval_coordinates_to_match_source_articulation_band_after_site_projection" --artifacts-path .\.testartifacts\reactive-dims-projected-red`
  - Failed with `Expected: 122 Actual: 124`.
- GREEN:
  - Same focused test passed 1/1.
- Regression:
  - `DimensionIntervalReactiveProjectorTests` passed 17/17.
  - Desktop `ApplyAutoFitPlan` tests passed 11/11.
  - Full Application tests passed 121/121.
  - `git diff --check` exited 0 with CRLF warnings only.
- Export boundary:
  - `BuildAdjustedSitePlanPlacement_exports_reactive_dimension_patch_from_projected_preview_coordinates` passed, proving projected preview cotas are mapped back into source-coordinate `AdjustedDimensions`.
  - `ExportAsync_patches_reactive_dimension_text_from_adjustment_preview` passed, proving the site-plan exporter patches DXF dimension text when it receives `AdjustedDimensions`.

## Tradeoff
The fix supports both coordinate spaces instead of introducing a new coordinate-space type. That is the minimal compatible change. If coordinate provenance grows, introduce explicit coordinate-space metadata instead of guessing from overlap.
