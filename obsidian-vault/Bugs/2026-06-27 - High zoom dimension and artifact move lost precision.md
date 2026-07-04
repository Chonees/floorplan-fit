---
type: Bugs
date: 2026-06-27
status: fixed
scope: Loop 1 Desktop preview edit
related:
  - [[2026-06-24 - High zoom manual floor-plan move stalled]]
---

# High zoom dimension and artifact move lost precision

## Problem
At very high zoom, moving dimensions or movable artifacts by tiny pointer deltas could feel stuck or imprecise.

## Root Cause
Desktop movement paths still used generic 3-decimal model rounding and a 0.001 source-unit persistence epsilon:

- `PreviewInteractionCoordinator.ResolveWorldDelta`
- `FloorPlanPreviewControl.ApplyAbsolutePointDelta` / `ApplyTranslationDelta`
- `DimensionPreviewProjector` active dimension edit deltas
- `NativeDimensionEditor` dimension geometry rebuilding

At a `10,000,000` viewport scale, a 10px drag is `0.000001` source units. Three-decimal rounding collapsed that movement to `0`, and the 0.001 epsilon made the release look non-meaningful.

## Fix
- Lowered movement persistence epsilon to `0.000001`.
- Kept movable artifact absolute/translation coordinates at 6-decimal movement precision.
- Kept edited dimension preview/model geometry at 6-decimal precision.
- Left measurement/display rounding at 3 decimals so labels do not become noisy.

## Verification
- RED: `HandlePointerReleased_commits_high_zoom_translation_precision` failed with a null committed move.
- RED: `BuildRenderedDimensions_preserves_high_zoom_dimension_edit_precision` failed with `224` instead of `224.000001`.
- GREEN: focused high-zoom tests passed `2/2`.
- GREEN: relevant preview suites passed `33/33`.
- Broader Desktop test run passed `262` tests and failed `10` unrelated source-path architecture tests that try to read `C:\Users\lucas\src\...` instead of this workspace path.
- `git diff --check` passed.

## Files
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/PreviewInteractionCoordinator.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/DimensionPreviewProjector.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/NativeDimensionEditor.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/PreviewInteractionCoordinatorTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/NativeDimensionPreviewControlTests.cs`
