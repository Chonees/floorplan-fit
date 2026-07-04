# Projected dependent DXF export scales circle and arc radii

## What changed
- `ProjectedPlanSheetDxfExporter` now scales DXF `CIRCLE` and `ARC` radius group code `40` using the approved sheet projection scale.
- The exporter still projects explicit DXF point pairs as before.
- Added coverage for dependent sheet symbols/arcs: center points are transformed and radii are scaled.

## Why
Electrical/roof/facade dependent sheets often contain circular symbols or arcs. A HousePlanSet package cannot be considered coherent if centers move/scale but radii remain at the old source size.

## Boundary
- This is not a full DXF entity engine.
- TEXT height and INSERT/block internals remain intentionally untouched until real dependent sheets prove the need.
- No per-sheet fit engine was introduced; this is still applying the approved projection transform to dependent artifacts.

## Verification
- Test-first RED: `ProjectedPlanSheetDxfExporterTests` expected scaled `CIRCLE` and `ARC` radius values and production only transformed coordinate pairs.
- GREEN: exporter tracks current entity type and scales code `40` only for `CIRCLE`/`ARC`.
- `git diff --check` passed for touched files.
- No `dotnet test` and no `dotnet build` were run due repository rule.