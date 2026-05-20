# Native dimension preview now renders exact lines and block-true text placement

## Lineage
- replaces: [[Implementation/2026-05-11 - Review preview now renders native dimension text]]

## What changed
- Promoted native dimensions from a text-only overlay to a geometry-backed preview overlay.
- `IxMiliaDimensionExtractor` now captures DXF geometry-block line segments plus exact MTEXT/TEXT placement metadata from `*D...` blocks.
- SQLite persists the new render payload in `extracted_dimensions` plus `extracted_dimension_line_segments`.
- Review now renders native dimension linework and uses the block's true text anchor/height/rotation instead of the old midpoint heuristic.

## Why this mattered
The previous preview proved the extraction path existed, but it was still CAD-loose: it showed only the label text and estimated the anchor from definition points. That was good enough to unblock visibility, but not good enough for precise review.

## Result
Review preview now shows dimensions with much higher CAD fidelity:
- line segments come from the dimension geometry block itself
- text placement comes from the block's own MTEXT/TEXT insertion point
- fallback midpoint math still exists only when exact block text placement is unavailable

## Key files
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaDimensionExtractor.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedDimensionRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/DimensionPreviewLayerRenderer.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/CadTextPreviewLayerRenderer.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`


## Operational note
- The live screenshot issue was verified against the real desktop workspace DB at `src/FloorplanFit.Desktop/bin/Debug/net10.0/workspace/app.db`.
- That DB still lacked `render_text_x` and `extracted_dimension_line_segments`, so the running app instance was still backed by stale pre-precision extraction data.
- Review opening is now hardened: if a version loads dimensions with no line segments and no exact render-text payload, the Desktop flow re-runs extraction automatically and reloads Review instead of leaving a misleading preview on screen.
