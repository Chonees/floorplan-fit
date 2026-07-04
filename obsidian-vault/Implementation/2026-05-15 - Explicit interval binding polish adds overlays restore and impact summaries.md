---
date: 2026-05-15
type: implementation
topic: native-dimensions
---

# Explicit interval binding polish: overlays, restore flow, and impact summaries

## What changed

- Added the final usability slice on top of explicit corridor/node/interval bindings:
  - restore/delete flow for manual interval bindings
  - preview overlays for articulation bands, selected corridors, selected nodes, and active measured interval
  - Fit-panel summaries for corridor suggestions from the selected pinch group and pinch-group impact on the selected dimension
- `PreviewRenderComposer` now renders `MeasurementBindingPreviewLayerRenderer` before dimensions/text so the user can see the measurement semantics directly on the canvas.
- `FloorPlanReviewViewModel` now exposes:
  - `CanRestoreSelectedDimensionIntervalBinding`
  - `RestoreSelectedDimensionIntervalBindingAsync(...)`
  - `SelectedPinchGroupImpactSummary`
  - `SelectedDimensionImpactSummary`
- Desktop Fit UI now includes `Restore Authored/Static Binding` and shows overlap-driven impact summaries.

## Why it matters

- The explicit binding system is now much more teachable and auditable in-product: the user can see the corridor, see the interval, see the reducible band, and restore a dimension back to authored/static state without touching SQLite manually.
- This keeps the runtime authority where we want it:
  - pinches define reducible bands
  - corridors/nodes define measured intervals
  - only manual-verified interval bindings react

## Files

- `src/FloorplanFit.Application/FloorPlans/Curation/RestoreDimensionIntervalBindingHandler.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/MeasurementBindingPreviewLayerRenderer.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderComposer.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderScene.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/PreviewSemanticPalette.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewMutationCoordinator.cs`
- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs`

## Verified

- Application:
  - `ArticulationBandProjectorTests`
  - `DimensionIntervalReactiveProjectorTests`
  - `DimensionIntervalBindingHandlersTests`
- Infrastructure:
  - `MeasurementIntervalBindingPersistenceIntegrationTests`
  - `FloorPlanReviewSessionReaderIntegrationTests`
- Desktop:
  - `MeasurementBindingPreviewLayerRendererTests`
  - `MeasurementBindingFloorPlanReviewViewModelTests`
  - `PreviewRenderComposerTests`
  - `FloorPlanPreviewControlTests`
  - broad regression slice over `NativeDimensionPreviewControlTests|FloorPlanReviewViewModelTests|DimensionEditingFloorPlanReviewViewModelTests|PreviewCollectionObserverHubTests`

## Remaining gap

- The current polish closes the first usable explicit-binding product loop, but pinch-group-to-corridor confirmation still behaves as a live overlap suggestion, not as a separately persisted user-confirmed map.
- That is acceptable for now because runtime authority still comes from `ManualVerified` dimension intervals, not from inferred dimension proximity.
