# 2026-07-01 - Desktop selects floor plan version before related plan import

## What
- Added a `Select` button to each floor-plan version row in the Library.
- The button calls `SelectVersionButton_OnClick`, resolves the owning library item, and delegates to `LibraryViewModel.SelectVersion(item, version)`.
- This makes the dependent sheet import target explicit before pressing `Import Sheet`.

## Why
The user correctly identified that the Library showed floor plans and an `Import Sheet` action, but lacked an obvious way to select which floor plan/version should receive the related electrical/roof/facade plan.

## Boundary
- No new wizard.
- No new registration engine.
- No per-sheet adjustment engine.
- This only exposes the already-existing selection state in the UI.

## Verification
- Added layout/cable assertion in `ReviewFloorPlanWindowLayoutTests`.
- Static RED check confirmed production was missing `SelectVersionButton_OnClick` before implementation.
- Static GREEN check confirmed XAML + code-behind wiring is present.
- Scoped `git diff --check` passed for touched files.
- No `dotnet build`/full test run due repository rule: `Never build after changes`.

## Files
- `src/FloorplanFit.Desktop/MainWindow.axaml`
- `src/FloorplanFit.Desktop/MainWindow.axaml.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
