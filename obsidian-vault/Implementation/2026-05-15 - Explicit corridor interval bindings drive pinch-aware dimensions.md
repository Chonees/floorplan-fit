---
date: 2026-05-15
type: implementation
topic: native-dimensions
---

# Explicit corridor interval bindings drive pinch-aware dimensions

## What changed

- Added explicit review-session artifacts for manual-verified measurement semantics:
  - `ArticulationBandDto`
  - `MeasurementCorridorDto`
  - `MeasurementNodeDto`
  - `DimensionIntervalBindingDto`
- Added SQLite/domain/repository support for:
  - `measurement_corridors`
  - `measurement_nodes`
  - `floorplan_dimension_interval_bindings`
- `SqliteFloorPlanReviewSessionReader` now hydrates corridors, nodes, manual interval bindings and projected articulation bands.
- `FloorPlanPreviewControl` now re-enables dimension reactivity only through `DimensionIntervalReactiveProjector`, and only when:
  - a pinch preview is active,
  - a matching `ArticulationBand` exists,
  - the dimension has a `ManualVerified` interval binding,
  - the interval overlaps the reducible band.
- Added Fit-tool UX to:
  - create local measurement corridors from selected preview geometry,
  - place measurement nodes by clicking supported preview geometry,
  - bind the selected native dimension to start/end nodes inside the selected corridor.

## Why it matters

- This replaces the unsafe proximity-based dimension reactivity that broke random dimensions during pinch preview.
- Runtime authority now starts to move toward:
  - **pinches define reducible bands**
  - **manual corridors/nodes define measured intervals**
  - **only manual-verified interval bindings react**

## Files

- `src/FloorplanFit.Contracts/FloorPlans/ArticulationBandDto.cs`
- `src/FloorplanFit.Contracts/FloorPlans/MeasurementCorridorDto.cs`
- `src/FloorplanFit.Contracts/FloorPlans/MeasurementNodeDto.cs`
- `src/FloorplanFit.Contracts/FloorPlans/DimensionIntervalBindingDto.cs`
- `src/FloorplanFit.Application/FloorPlans/Review/ArticulationBandProjector.cs`
- `src/FloorplanFit.Application/FloorPlans/Review/DimensionIntervalReactiveProjector.cs`
- `src/FloorplanFit.Application/FloorPlans/Curation/AddMeasurementCorridorHandler.cs`
- `src/FloorplanFit.Application/FloorPlans/Curation/AddMeasurementNodeHandler.cs`
- `src/FloorplanFit.Application/FloorPlans/Curation/SaveDimensionIntervalBindingHandler.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteMeasurementCorridorRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteMeasurementNodeRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteDimensionIntervalBindingRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`

## Verified

- Application:
  - `ArticulationBandProjectorTests`
  - `DimensionIntervalReactiveProjectorTests`
- Infrastructure:
  - `MeasurementIntervalBindingPersistenceIntegrationTests`
  - `FloorPlanReviewSessionReaderIntegrationTests`
- Desktop:
  - `BuildRenderedDimensionsForPreview_reprojects_manual_verified_interval_bindings_when_the_selected_pinch_group_overlaps`
  - `MeasurementBindingFloorPlanReviewViewModelTests`
  - `PreviewCollectionObserverHubTests`
  - broad regression slice over `NativeDimensionPreviewControlTests|FloorPlanReviewViewModelTests|DimensionEditingFloorPlanReviewViewModelTests`

## Remaining gap

- This slice gives us the new explicit runtime seam and the first usable UX for corridors/nodes/bindings.
- It does **not** yet provide:
  - rich preview overlays for selected intervals/corridors,
  - explicit confirm/override UX for pinch-group-to-corridor impact suggestions,
  - restore/delete flows for manual interval bindings.
