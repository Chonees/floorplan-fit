# Fit pinch groups panel and handle gating

## Context
The Fit pinch workflow was confusing: the preview showed a green articulation rectangle the user did not want, and Height handles could appear even when the selected group/axis had no markers to drive a compression preview. The right panel also listed individual pinch markers globally instead of grouping them like the franja workflow.

## Implementation
- Removed the `RenderArticulationBand(...)` call from `MeasurementBindingPreviewLayerRenderer.Render`, so the green band rectangle is not painted.
- Added marker-aware handle gating in `CompressionHandlePreviewLayerRenderer`: handles are visible only when there is a selected pinch group with at least one marker matching the active axis.
- Updated `FloorPlanPreviewControl.ResolveEdgeDrag` to use the same visible-handle set, preventing invisible/invalid drag targets.
- Changed `PinchMarkerPreviewLayerRenderer.FilterForPreviewGroup` so preview compression has no active markers when no group is selected.
- Added `SelectedPinchGroupMarkers` and `CanRemoveSelectedPinch` to `FloorPlanReviewViewModel`.
- Changed `AddPinchGroupAsync` to auto-name groups as `Ajuste N`, removing the dependency on a manual name field.
- Reworked `FitExistingPanel` XAML: `Grupos de pinches` selector, `Crear grupo de pinches` button, and scoped `Pinches de este grupo` list.

## Tests
- RED confirmed: `GetVisibleHandles` lacked group/marker-aware inputs; `SelectedPinchGroupMarkers` did not exist.
- GREEN: focused preview/layout/VM tests passed 107/107.
- GREEN: full Desktop test project passed 183/183 with `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --nologo --artifacts-path .testartifacts\dotnet-test-artifacts-pinch-ux-desktop-all`.

## Notes
The green rectangle was only visual noise. Removing it does not delete articulation-band data; bands still exist for the adjustment model. The important UX invariant is now: if a handle is visible, dragging it has a selected group + axis markers capable of driving the preview.
