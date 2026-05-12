# Reactive dimension preview follows live geometry associations

## What
- Moved measurable-edge and dimension-association projectors into the Application layer as shared pure logic.
- Added a reactive dimension projector that recalculates non-manually-edited native dimensions from live measurable-edge anchors.
- Wired the preview control to derive dimensions from the current preview geometry instead of only from persisted extraction snapshots.

## Why
- Real-time associative behavior must follow the active geometry in memory, not only the last persisted review session.
- Pinch previews and live artifact translations need dimensions to react before the user commits changes.

## Where
- `src/FloorplanFit.Application/FloorPlans/Review/MeasurableEdgeProjector.cs`
- `src/FloorplanFit.Application/FloorPlans/Review/DimensionAssociationProjector.cs`
- `src/FloorplanFit.Application/FloorPlans/Review/ReactiveDimensionProjector.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`

## Outcome
- Associated native dimensions now react in preview when the underlying preview geometry changes.
- This currently targets inferred, non-manually-edited native dimensions in the live preview path.
- Persisted geometry mutation and authored association truth are still future slices.
