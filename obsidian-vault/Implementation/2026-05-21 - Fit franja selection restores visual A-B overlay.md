# Fit franja selection restores visual A-B overlay

replaces: [[2026-05-21 - Fit hides group names and shows selected franja nodes]]
replaced_by: [[2026-05-21 - Fit selection survives ComboBox refresh nulls]]

## Type
Bugfix

## Date
2026-05-21

## Context
After hiding internal group names and switching selected franja nodes to a visible list, selecting a franja could still fail visually: the preview did not always show the A/B node line/relationship unless a dimension was also selected. The user reported that selecting one node made the visual relation disappear.

## Root cause
`TryAutoAssignMeasurementEndpoints` required both `SelectedDimension` and `SelectedMeasurementCorridor`. That was wrong for preview/curation navigation. A franja with exactly two nodes already has enough information to show the A/B overlay, even before choosing which cota to bind.

## What changed
- Removed the `SelectedDimension` requirement from endpoint auto-assignment.
- A selected franja/corridor with exactly two nodes now assigns `SelectedMeasurementStartNode` and `SelectedMeasurementEndNode` immediately.
- Selecting an individual node from `Nodos de esta franja` only changes `SelectedMeasurementNode`; it preserves the selected franja and A/B endpoints.

## Why
The selected group/franja is not just a form row. It is visual context: the operator must see the node membership and the A/B line before deciding which dimension to save.

## Verification
- RED: `Measurement_group_selection_without_dimension_keeps_preview_nodes_and_interval_active` failed because selected start/end were null after group selection.
- GREEN: the new test passed after removing the `SelectedDimension` guard.
- Focused Desktop slice passed 68/68, including:
  - `ReviewFloorPlanWindowLayoutTests`
  - `FloorPlanReviewViewModelArchitectureTests`
  - `MeasurementBindingFloorPlanReviewViewModelTests`
  - `FloorPlanReviewViewModelTests`
  - `AppXamlInitializationTests`
  - `MeasurementBindingPreviewLayerRendererTests`

## Files
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/MeasurementBindingFloorPlanReviewViewModelTests.cs`
