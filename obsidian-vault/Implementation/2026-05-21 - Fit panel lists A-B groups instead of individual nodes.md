# Fit panel lists A-B groups instead of individual nodes

> Partially superseded: groups remain the main list, but a scoped `Nodo del grupo` dropdown was restored so the operator can choose A or B within the selected group. See [[2026-05-21 - Fit A-B groups keep scoped endpoint dropdown]].

replaces: [[2026-05-20 - Fit right panel restores node A-B binding controls]]
partially_replaced_by: [[2026-05-21 - Fit A-B groups keep scoped endpoint dropdown]]

## Type
Implementation

## Date
2026-05-21

## Context
The restored A/B node workflow was functionally correct, but still too granular for the operator. The user wanted the panel to show groups of A and B, not list each A and B node separately.

## What changed
- Renamed the right-panel section from `Nodos existentes` to `Grupos de A y B`.
- Added `MeasurementNodePairOptionViewModel`.
- Added `MeasurementNodePairOptions` and `SelectedMeasurementNodePairOption` to the Review ViewModel.
- Each A/B group is produced from a measurement corridor with at least two nodes.
- Selecting a group sets the corridor, highlighted node, start node, and end node together.
- Replaced the two individual A/B ComboBoxes with read-only summaries for A and B. Later updated to also add a scoped endpoint dropdown for A/B within the selected group.
- Removed the now-unused individual node option ViewModel and wrapper properties.

## Why
The user needs to pick the measurement relationship as one unit. A and B are not independent navigation objects in this workflow; they are a pair that describes what the cota measures.

## Verification
- RED confirmed: tests failed because `MeasurementNodePairOptions`, `SelectedMeasurementNodePairOption`, and A/B summary properties did not exist.
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
- `src/FloorplanFit.Desktop/ViewModels/MeasurementNodePairOptionViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/MeasurementBindingFloorPlanReviewViewModelTests.cs`
