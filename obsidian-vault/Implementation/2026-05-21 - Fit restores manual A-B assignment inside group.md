# Fit restores manual A-B assignment inside group

replaces: [[2026-05-21 - Fit A-B groups keep scoped endpoint dropdown]]
replaced_by: [[2026-05-21 - Fit hides group names and shows selected franja nodes]]

## Type
Bugfix / UX correction

## Date
2026-05-21

## Context
The user clarified the real regression: before, the operator could place nodes, then use two lower controls to decide which created node was A and which was B. The previous endpoint-dropdown fix only changed navigation/highlight and did not restore manual A/B assignment. It also made the group selector empty until two nodes existed.

## What changed
- Replaced pair-only options with `MeasurementNodeGroupOptions`, one option per measurement corridor/group regardless of node count.
- Added `MeasurementNodeGroupOptionViewModel`.
- Added `MeasurementNodeGroupNodeOptionViewModel` for scoped node labels.
- `Grupo A/B` now selects the corridor/group even with 0 nodes.
- `Nodo del grupo` lists nodes in that selected group for navigation/highlight.
- Restored lower `A` and `B` ComboBoxes bound to `SelectedMeasurementStartNodeOption` and `SelectedMeasurementEndNodeOption`.
- A/B dropdowns are scoped to the selected group and show `Nodo 1`, `Nodo 2`, etc. plus source/axis/coordinate/line details.
- Removed the pair-only/endpoint-only ViewModel files from the current implementation.

## Why
The correct workflow is: choose group/franja, place nodes, then assign which placed node is A and which is B before saving what the dimension measures. A/B assignment is curation state, not just endpoint navigation.

## Verification
- RED confirmed: focused tests failed because group/manual A-B option properties did not exist.
- GREEN focused slice passed 6/6.
- Full focused Desktop slice passed 63/63:
  - `ReviewFloorPlanWindowLayoutTests`
  - `FloorPlanReviewViewModelArchitectureTests`
  - `MeasurementBindingFloorPlanReviewViewModelTests`
  - `FloorPlanReviewViewModelTests`
  - `AppXamlInitializationTests`

## Files
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/ViewModels/MeasurementNodeGroupOptionViewModel.cs`
- `src/FloorplanFit.Desktop/ViewModels/MeasurementNodeGroupNodeOptionViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/MeasurementBindingFloorPlanReviewViewModelTests.cs`

## Follow-up: single-node groups and deletion
- `Grupo A/B` is confirmed to list the selected corridor/group even with 0 or 1 node.
- Added an `Eliminar franja` button bound to `CanRemoveSelectedMeasurementCorridor` and `RemoveMeasurementCorridorButton_OnClick`.
- Added test coverage that a one-node group can arm `Elegir nodo` again, preserving the workflow of placing a second node later.
- Focused Desktop verification passed 63/63 after the follow-up.
