# Fit A-B groups keep scoped endpoint dropdown

> Superseded: endpoint-only navigation was not the intended old workflow. The current UI restores manual A/B assignment dropdowns inside the selected group. See [[2026-05-21 - Fit restores manual A-B assignment inside group]].

replaces: [[2026-05-21 - Fit panel lists A-B groups instead of individual nodes]]
replaced_by: [[2026-05-21 - Fit restores manual A-B assignment inside group]]

## Type
Implementation

## Date
2026-05-21

## Context
After moving the panel to A/B groups, the user clarified that we removed too much: they still need the dropdown that lets them choose which endpoint inside the group is active. The issue was not the dropdown itself; the issue was showing raw coordinates without A/B labels and listing nodes globally.

## What changed
- Kept `Grupos de A y B` as the main group selector.
- Added `MeasurementNodePairEndpointOptionViewModel`.
- Added `SelectedMeasurementNodePairEndpointOptions` and `SelectedMeasurementNodePairEndpointOption` to the Review ViewModel.
- Added a scoped `Nodo del grupo` ComboBox under the selected group.
- Endpoint options are labeled `A` and `B`, and details include source artifact kind, axis, axis coordinate, and line ratio.
- Selecting A or B changes only the active/highlighted `SelectedMeasurementNode`; it does not change the pair's saved start/end nodes.

## Why
The operator needs to pick the active endpoint within the selected group while still thinking in grouped A/B relationships. This preserves navigation and avoids going back to a raw global node list.

## Verification
- RED confirmed: tests failed because `SelectedMeasurementNodePairEndpointOptions` and `SelectedMeasurementNodePairEndpointOption` did not exist.
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
- `src/FloorplanFit.Desktop/ViewModels/MeasurementNodePairEndpointOptionViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/MeasurementBindingFloorPlanReviewViewModelTests.cs`

## Follow-up clarification
The user clarified that the original request referred to the old lower A/B controls used while adding new nodes, not the endpoint dropdown interpretation. However, the current `Grupos de A y B` + scoped `Nodo del grupo` UX is accepted and should be left unchanged for now.
