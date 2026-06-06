# Fit right panel restores node A-B binding controls

> Partially superseded: the right panel still keeps A/B binding, but no longer lists individual A and B nodes in separate dropdowns. See [[2026-05-21 - Fit panel lists A-B groups instead of individual nodes]].

replaces: [[2026-05-20 - Fit tools use canvas-first icon palette]]
partially_replaced_by: [[2026-05-21 - Fit panel lists A-B groups instead of individual nodes]]

## Type
Implementation

## Date
2026-05-20

## Context
The popup/dropdown cleanup went too far. It correctly kept the canvas-first toolbar and moved `Crear franja` into a Flyout, but it removed the useful parts of the old dimension-binding workflow: named node navigation, explicit A/B node selection, and `Guardar que mide`.

## What changed
- Kept the top `FitToolPalette` minimal: axis, mm, Marcar pinch, Crear franja, Elegir nodo, Quitar pinch.
- Restored the right `FitExistingPanel` as a navigation/binding panel, not a management console.
- Added `MeasurementNodeOptionViewModel` so node dropdowns show operator-readable names like `Patio-Width - Nodo A` and details like `WallCandidate - Width - eje 100 - linea 0.5`.
- Added `MeasurementNodeOptions`, `SelectedMeasurementCorridorNodeOptions`, and selected option wrappers in `FloorPlanReviewViewModel`.
- Selecting any existing node now selects its corridor automatically, so the A/B dropdowns and save-enabled state follow the chosen node.
- Restored `Nodo A`, `Nodo B`, `Cota vinculada`, `Guardar que mide`, and `Volver a medida fija` in the right panel.

## Why
The user needs fast curation, not a stripped-down UI that hides necessary state. The toolbar is for canvas actions; the right panel is for reviewing existing objects and saving what a dimension measures.

## Verification
- RED confirmed with `dotnet test ... --filter "FullyQualifiedName~ReviewFloorPlanWindowLayoutTests|FullyQualifiedName~MeasurementBindingFloorPlanReviewViewModelTests.Existing_node_options_show_corridor_names_and_drive_a_b_dimension_binding_selection"`: missing `MeasurementNodeOptions` / selected option properties.
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
- `src/FloorplanFit.Desktop/ViewModels/MeasurementNodeOptionViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/MeasurementBindingFloorPlanReviewViewModelTests.cs`
