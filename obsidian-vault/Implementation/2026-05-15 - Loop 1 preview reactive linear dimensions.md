---
created: 2026-05-15
project: floorplan-fit
type: implementation
status: active
replaces:
replaced_by:
---

# Loop 1 preview reactive linear dimensions

## What
Implemented the first live preview wiring for associative native dimensions: when pinch preview is active, Desktop now rebuilds live measurable edges from transformed `PreviewGeometry` and feeds them into the reactive dimension projector before rendering.

## Why
Loop 0 only established semantic truth. The next step had to prove that a changed preview geometry can move affected linear dimensions in real time while leaving unresolved dimensions authored/static.

## Where
- `src/FloorplanFit.Desktop/Controls/Preview/DimensionPreviewProjector.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewSessionCoordinator.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `tests/FloorplanFit.Desktop.Tests/Controls/NativeDimensionPreviewControlTests.cs`

## Verified behavior
1. Active pinch preview now recalculates live `MeasurableEdges` from the already-transformed `PreviewGeometry`.
2. Resolved linear dimensions reproject through `ReactiveDimensionProjector` before any active manual preview edit is applied.
3. Unresolved dimensions stay authored/static even when preview geometry changes.
4. `MeasurementContext` now flows from review session -> ViewModel -> preview control so Desktop has the conversion/tolerance inputs required for live edge rebuilding.

## Verified tests
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "BuildRenderedDimensionsForPreview"`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter NativeDimensionPreviewControlTests`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter FloorPlanReviewViewModelTests`

## Learned
- The minimal safe Desktop seam was `DimensionPreviewProjector`: reactive reprojection belongs there because it already arbitrates between base dimensions and active edit previews.
- Verification in this repo can be blocked by a running `FloorplanFit.Desktop.exe`; when focused Desktop tests fail with `MSB3027/MSB3021`, check for a locked executable before assuming the slice is broken.
