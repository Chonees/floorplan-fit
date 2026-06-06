# Fit selection survives ComboBox refresh nulls

replaces: [[2026-05-21 - Fit franja selection restores visual A-B overlay]]
replaced_by: [[2026-05-21 - Fit franja dropdown uses stable option instances]]

## Type
Bugfix

## Date
2026-05-21

## Context
The user reported that after selecting a group/franja, A/B appeared in the panel but nodes were not highlighted in the preview. They also reported that the same dropdown became empty and could no longer be used to navigate.

## Root cause
The ViewModel option setters treated `SelectedItem = null` as an intentional clear. Avalonia ComboBox/ListBox can push `null` transiently when `ItemsSource` refreshes after selection notifications. That wiped `SelectedMeasurementCorridor`, `SelectedMeasurementNode`, or A/B endpoints, which in turn removed the ids consumed by the preview overlay.

The preview also used nested nullable bindings (`SelectedMeasurementStartNode.NodeId`, etc.), which is more fragile for rapid selection changes than binding to explicit id properties.

## What changed
- `SelectedMeasurementNodeGroupOption = null` no longer clears the selected franja.
- `SelectedMeasurementGroupNodeOption = null` no longer clears selected node navigation.
- `SelectedMeasurementStartNodeOption = null` and `SelectedMeasurementEndNodeOption = null` no longer clear A/B endpoints.
- Added explicit id properties on `FloorPlanReviewViewModel`:
  - `SelectedMeasurementCorridorId`
  - `SelectedMeasurementNodeId`
  - `SelectedMeasurementStartNodeId`
  - `SelectedMeasurementEndNodeId`
- Updated `ReviewFloorPlanWindow.axaml` so `FloorPlanPreviewControl` binds to those explicit ids.

## Why
The user-facing selection controls are navigational. A transient UI null during refresh is not the same as the user deleting/restoring a franja. Destructive clears should happen through explicit delete/restore flows, not through ComboBox refresh mechanics.

## Verification
- RED: tests failed/compiled red until explicit id properties existed and transient nulls stopped clearing selection.
- GREEN focused Desktop slice passed 68/68:
  - `ReviewFloorPlanWindowLayoutTests`
  - `FloorPlanReviewViewModelArchitectureTests`
  - `MeasurementBindingFloorPlanReviewViewModelTests`
  - `FloorPlanReviewViewModelTests`
  - `AppXamlInitializationTests`
  - `MeasurementBindingPreviewLayerRendererTests`

## Files
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/MeasurementBindingFloorPlanReviewViewModelTests.cs`
