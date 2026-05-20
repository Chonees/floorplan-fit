# Native dimension architecture modularized for associativity groundwork

## What
- Split native dimension interaction responsibilities out of `FloorPlanPreviewControl` into dedicated preview helpers.
- Split review-session dimension/measurable-edge projection logic out of `SqliteFloorPlanReviewSessionReader` into dedicated persistence projectors.

## Why
- The preview control and review-session reader had absorbed too much native-dimension behavior.
- Associative real-time dimensions need cleaner seams between interaction, projection, and persistence before adding dependency graphs.

## Where
- `src/FloorplanFit.Desktop/Controls/Preview/NativeDimensionEditor.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/NativeDimensionHitTester.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/DimensionPreviewLayerRenderer.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Infrastructure/Persistence/ResolvedFloorPlanDimensionProjector.cs`
- `src/FloorplanFit.Infrastructure/Persistence/MeasurableEdgeProjector.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`

## Outcome
- Native dimension editing now has a cleaner split between:
  - hit-testing
  - handle/snapping/edit geometry
  - rendering
  - review-session override projection
  - measurable-edge projection
- This keeps current behavior intact while preparing the codebase for future associativity work (`dimension -> anchor -> measurable edge -> wall/opening/space`).
