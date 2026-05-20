# Review preview now renders native dimension text

## What changed
- Wired `Dimensions` into `FloorPlanPreviewControl` and `ReviewFloorPlanWindow.axaml`.
- Added dimension text rendering in `CadTextPreviewLayerRenderer`.
- Computed preview anchor positions from DXF definition points so dimension text lands on the dimension line midpoint instead of floating arbitrarily.

## Root cause
The first native-dimension slice persisted and listed dimensions in the Review Queue, but never bound or rendered them in the preview canvas.

## Result
Review now shows native dimension text in both places:
- left-side `Dimensions` queue
- central visual preview overlay

## Key files
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/CadTextPreviewLayerRenderer.cs`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`

## Status
- superseded: yes
- replaced_by: [[Implementation/2026-05-11 - Native dimension preview now renders exact lines and block-true text placement]]
