# Fit hides group names and shows selected franja nodes

replaces: [[2026-05-21 - Fit restores manual A-B assignment inside group]]

## Type
Bugfix / UX correction

## Date
2026-05-21

## Context
The user rejected the current Fit panel because it still exposed group naming/copy and made selected franja nodes feel hidden. The intended workflow is simpler: choose a franja from `Grupos de A y B`, immediately see the nodes belonging to it, keep A/B assignment controls, and delete unwanted franjas.

## What changed
- Removed the toolbar flyout/text field that asked for `Nombre de franja`.
- `Crear franja` is now a direct icon action; the ViewModel generates the next internal name (`Franja 1`, `Franja 2`, etc.) automatically.
- The group selector no longer displays stored corridor names like `Patio-Width`; it shows neutral `Franja N` labels plus axis/node count details.
- Replaced the `Nodo del grupo` dropdown with a visible `Nodos de esta franja` list bound to `SelectedMeasurementGroupNodeOptions`.
- Kept `Eliminar franja` on the selected franja and kept lower A/B dropdowns scoped to the selected franja.
- Status messages for node placement/delete no longer surface internal franja names.

## Why
The operator does not care about internal group names during curation. The UX should expose the real task objects: franjas, their nodes, and A/B assignment. Names are internal bookkeeping, not a required manual step.

## Verification
- RED: focused tests failed because the XAML still had the name flyout / `Nodo del grupo`, ViewModel still required `NewMeasurementCorridorName`, and group options still displayed `Patio-Width`.
- GREEN: focused Desktop slice passed 63/63:
  - `ReviewFloorPlanWindowLayoutTests`
  - `FloorPlanReviewViewModelArchitectureTests`
  - `MeasurementBindingFloorPlanReviewViewModelTests`
  - `FloorPlanReviewViewModelTests`
  - `AppXamlInitializationTests`

## Files
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/MeasurementBindingFloorPlanReviewViewModelTests.cs`
