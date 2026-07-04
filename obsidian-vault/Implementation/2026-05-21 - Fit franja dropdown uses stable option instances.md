# Fit franja dropdown uses stable option instances

replaces: [[2026-05-21 - Fit selection survives ComboBox refresh nulls]]

## Type
Bugfix

## Date
2026-05-21

## Context
The user still saw the franja selection break: selecting a group/franja could make the dropdown blank and preview navigation/highlight unreliable. The earlier null-guard prevented destructive clears, but the selected item still did not remain attached to the current ItemsSource.

## Root cause
`MeasurementNodeGroupOptions` and `SelectedMeasurementGroupNodeOptions` were computed getters that created a new array of new record instances every time the binding engine read them. Avalonia selection controls work best when `SelectedItem` is the same object instance present in `ItemsSource`. Value-equal record instances were not enough.

## What changed
- Added a cached stable list for `MeasurementNodeGroupOptions`.
- Added a cached stable list for `SelectedMeasurementGroupNodeOptions`, keyed by selected corridor/franja id.
- Invalidates group/node option caches only when underlying review session data refreshes, and invalidates the selected node list when the selected corridor changes.
- Regression test now asserts reference identity (`Assert.Same`) between selected options and current ItemsSource options.

## Why
For UI selection, identity matters. The operator must be able to choose a franja and keep navigating from the same dropdown/list without the control losing its selected item.

## Verification
- RED: `Assert.Same` failed because selected option and current ItemsSource option were different instances.
- GREEN: focused Desktop slice passed 68/68 after stable option caching.

## Files
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/MeasurementBindingFloorPlanReviewViewModelTests.cs`
