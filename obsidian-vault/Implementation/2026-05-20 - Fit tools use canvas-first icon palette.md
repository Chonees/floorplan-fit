# Fit tools use canvas-first icon palette

> Partially superseded: the final "right panel only two dropdowns" simplification removed too much. The current panel restores node A/B selection and Guardar que mide. See [[2026-05-20 - Fit right panel restores node A-B binding controls]].

replaces: [[2026-05-20 - Fit tools moved into dedicated workbench column]]
partially_replaced_by: [[2026-05-20 - Fit right panel restores node A-B binding controls]]

## Type
Implementation

## Date
2026-05-20

## Context
The first UX fix removed the long Fit scroll but created a heavy right-column workbench. The user rejected it as too console-like and asked for something more intuitive: icons where each icon is a tool.

## What changed
- Removed the `FitWorkbench` column.
- Removed the extra `IsGeneralInspectorSelected` ViewModel flag and notification.
- Added `FitToolPalette` directly above the preview canvas, visible only when the Fit rail tool is active.
- Kept the existing visual system by reusing `Button.tool`, `Button.tool-active`, section-card styling, and the existing right rail.
- Follow-up visual QA simplified the palette further:
  - The top palette now only keeps axis, max trim `mm`, and direct canvas icon actions.
  - The top palette no longer contains long help text, list dropdowns, A/B selectors, or a disabled `Vincular cota` icon.
  - Existing objects and slower configuration now live in the right `FitExistingPanel`.
- Second follow-up simplified the right panel:
  - `Crear franja` now opens a `Flyout` popup from its toolbar icon and asks for `Nombre de franja`.
  - The right `FitExistingPanel` now shows only two dropdowns: all existing pinches and all existing nodes.
  - User-facing group controls were removed from this screen; groups remain internal implementation detail.
- Top icon actions now are:
  - Marcar pinch
  - Crear franja
  - Elegir nodo
  - Quitar pinch
- Right panel controls now include:
  - Pinche existente dropdown
  - Nodo existente dropdown

## Why
Fit curation is a canvas-first workflow. The user should look at the plan, pick a tool, and click geometry—not scroll through a form.

## Verification
- RED confirmed: tests failed against the workbench implementation because `FitToolPalette` did not exist and `IsGeneralInspectorSelected` still existed.
- GREEN confirmed: layout/architecture tests passed 12/12.
- Focused Desktop slice passed 62/62:
  - `ReviewFloorPlanWindowLayoutTests`
  - `FloorPlanReviewViewModelArchitectureTests`
  - `MeasurementBindingFloorPlanReviewViewModelTests`
  - `FloorPlanReviewViewModelTests`
  - `AppXamlInitializationTests`
- Follow-up cleanup also passed the same focused Desktop slice 62/62.
- Second follow-up popup/dropdown cleanup passed the same focused Desktop slice 62/62.

## Files
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewNotificationCoordinator.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelArchitectureTests.cs`
