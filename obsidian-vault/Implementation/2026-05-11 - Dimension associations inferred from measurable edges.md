# Dimension associations inferred from measurable edges

## What
- Added review-session contract data for `DimensionAssociations` and `DimensionAnchorReference`.
- Added an infrastructure projector that infers native dimension endpoint associations against measurable-edge anchors.
- Surfaced association state in the Review inspector for selected dimensions.

## Why
- Real-time associative dimensions need an explicit relation layer between native DIMENSION entities and editable plan geometry.
- Without this relation layer, moving walls/spaces can never safely propagate into dimension recalculation.

## Where
- `src/FloorplanFit.Contracts/FloorPlans/DimensionAssociationDto.cs`
- `src/FloorplanFit.Contracts/FloorPlans/DimensionAnchorReferenceDto.cs`
- `src/FloorplanFit.Contracts/FloorPlans/FloorPlanReviewSessionDto.cs`
- `src/FloorplanFit.Infrastructure/Persistence/DimensionAssociationProjector.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Review/DimensionAssociationProjectorTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/DimensionEditingFloorPlanReviewViewModelTests.cs`

## Outcome
- Native dimensions now expose a first-pass inferred link to measurable-edge anchors.
- The association is still inferred, not yet persisted as authored dependency truth.
- This is the groundwork for the next serious slice: runtime geometry edit sessions that recalculate dimensions from associated anchors in memory.
