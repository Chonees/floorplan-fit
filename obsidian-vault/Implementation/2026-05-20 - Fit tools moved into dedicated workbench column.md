# Fit tools moved into dedicated workbench column

> Superseded: this workbench-column approach was rejected because it still felt like a console/panel. Replaced by [[2026-05-20 - Fit tools use canvas-first icon palette]].

replaced_by: [[2026-05-20 - Fit tools use canvas-first icon palette]]

## Type
Implementation

## Date
2026-05-20

## Context
The review UI had a professional workflow problem: pinches, max trim mm, measurement corridors, nodes, and dimension bindings were all stacked inside one long Inspector scroll. That made curation slow because the operator had to scroll, touch, scroll, touch, and lose context.

## What changed
- Added a dedicated `FitWorkbench` area for the Fit tool.
- Kept generic selection tools inside `GeneralInspectorContent`.
- Added `IsGeneralInspectorSelected` to the Review ViewModel so the XAML can switch between the generic inspector and the Fit workbench cleanly.
- Changed the main review columns to `320,*,420,72` to give the Fit workbench enough width for professional use.
- Reorganized Fit into bounded sections:
  - Ajuste rápido: group name, axis, mm, and add-pinch action stay at the top.
  - Pinches: groups and placed markers.
  - Bandas: computed articulation bands and impact summary.
  - Franjas: measurement corridors.
  - Nodos: selected corridor nodes and point placement.
  - Cota vinculada: start/end node selection and save/restore binding.

## Why
The Fit workflow is not a generic property inspector. It is an operator console. Keeping it in a long vertical scroll was the UX bottleneck.

## Verification
- Added layout contract test: `Fit_tool_uses_a_dedicated_workbench_instead_of_a_single_long_scroll`.
- Added architecture test for `IsGeneralInspectorSelected` and notification wiring.
- Focused Desktop verification passed: 62/62 tests.

## Files
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewNotificationCoordinator.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelArchitectureTests.cs`
