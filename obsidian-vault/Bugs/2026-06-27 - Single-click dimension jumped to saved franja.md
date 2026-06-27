---
type: Bugs
date: 2026-06-27
status: fixed
scope: Loop 1 measurement binding UX
related:
  - [[2026-06-27 - Measurement corridor allowed more than two nodes]]
---

# Single-click dimension jumped to saved franja

## Problem
Clicking a dimension/cota in the preview immediately selected the franja from its saved interval binding. That was disruptive while editing another franja because a normal click to select/move a dimension pulled the user back to the old franja.

## Root Cause
`FloorPlanReviewViewModel.OnSelectedDimensionChanged` always called `SelectSavedMeasurementBindingForDimension`, so every dimension selection navigated to the saved measurement corridor.

## Fix
- Single click now only selects the dimension and preserves the current franja/node selection.
- Double click uses Avalonia `ClickCount >= 2` to also navigate to the dimension's saved franja/nodes.
- The saved binding summary still shows the saved franja, even when the current selected franja is preserved.

## Verification
- RED: new single-click regression initially failed/errored because the API still only supported auto-navigation.
- GREEN: single-click/double-click regression tests passed `2/2`.
- GREEN: `MeasurementBindingFloorPlanReviewViewModelTests` + `DimensionEditingFloorPlanReviewViewModelTests` passed `29/29`.
- Broader run including `FloorPlanPreviewControlTests` passed `90` tests and failed `3` unrelated source-path tests reading `C:\Users\lucas\src\...`.
- `git diff --check` passed.

## Files
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/MeasurementBindingFloorPlanReviewViewModelTests.cs`
