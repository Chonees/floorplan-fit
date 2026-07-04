---
project: floorplan-fit
repo: https://github.com/Chonees/floorplan-fit
status: active
updated: 2026-05-15
loop: 4
area: dimensions-ordinate
---

# 2026-05-15 - Loop 4 typed ordinate bindings and reactive safety guardrails

## What
Started Loop 4 with the correct foundational slice: ordinate dimensions now produce typed semantic bindings (`OrdinateX` / `OrdinateY`) with axis-projected measured spans, and the reactive preview/export path is now binding-aware so non-linear bindings stay authored/static until a native typed ordinate rebuild exists.

## Why
Before this slice, ordinate dimensions were classified but treated as unsupported in `DimensionBindingProjector`, while the reactive projector was still association-only and therefore could eventually deform any resolved non-linear dimension as if it were linear. That was unsafe for the next typed loops.

## Files
- `src/FloorplanFit.Application/FloorPlans/Review/DimensionBindingProjector.cs`
- `src/FloorplanFit.Application/FloorPlans/Review/ReactiveDimensionProjector.cs`
- `src/FloorplanFit.Application/FloorPlans/Curation/ExportAdjustedDxfHandler.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/DimensionPreviewProjector.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/PreviewCollectionObserverHub.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewSessionCoordinator.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `tests/FloorplanFit.Application.Tests/FloorPlans/Review/DimensionBindingProjectorTests.cs`
- `tests/FloorplanFit.Application.Tests/FloorPlans/Review/ReactiveDimensionProjectorTests.cs`
- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/ExportAdjustedDxfHandlerTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/NativeDimensionPreviewControlTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/PreviewCollectionObserverHubTests.cs`

## Verification
- `dotnet test .\\tests\\FloorplanFit.Application.Tests\\FloorplanFit.Application.Tests.csproj --filter "DimensionBindingProjectorTests|ReactiveDimensionProjectorTests|ExportAdjustedDxfHandlerTests"`
- `dotnet test .\\tests\\FloorplanFit.Desktop.Tests\\FloorplanFit.Desktop.Tests.csproj --filter "NativeDimensionPreviewControlTests|PreviewCollectionObserverHubTests|FloorPlanReviewViewModelTests|DimensionEditingFloorPlanReviewViewModelTests"`

## Notes
- Ordinates now have semantic truth, but not yet a native reactive geometry rebuild.
- Preview/export remain safe because binding-aware projection only reprojects `LinearSpan` for now.
