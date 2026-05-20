> Superseded on 2026-05-20. Replaced by: $replacement. The active dimension interval rebuild path now uses the raw A/B node line; corridor/pinch band overlap remains axis-tagged.\n\n# 2026-05-18 - Axis-projected interval bindings fix cross-wall width and height spans

## Big Picture
Manual interval bindings now behave as true Width/Height corridor intervals instead of raw 2D chords between clicked endpoints. This fixes total-width/total-height bindings across different walls when the two clicks are not perfectly aligned.

## What changed
- Added a shared `DimensionAxisClassifier` so Application and Desktop agree on `Width`, `Height`, and `FreeAngle` semantics.
- `DimensionIntervalReactiveProjector` now refuses `FreeAngle` bindings and projects live start/end points back onto the authored corridor axis before rebuilding the dimension.
- `MeasurementBindingPreviewLayerRenderer` now renders the active interval axis-aligned, using the highlighted dimension baseline when available and the corridor guide baseline as fallback.
- `FloorPlanReviewViewModel` now disables `Guardar qué mide` for `FreeAngle` dimensions and explains the restriction in Spanish. It also blocks corridor-axis mismatches from being saved.

## Why
The previous runtime accepted cross-wall nodes inside one corridor but rebuilt the associated dimension from raw 2D points, which turned total-width / total-height bindings into diagonals whenever the user clicked at different heights or widths.

## Tests
- Application: cross-wall width misaligned, cross-wall height misaligned, and `FreeAngle` stays authored/static.
- Desktop: overlay projects to the correct baseline and free-angle bindings cannot be saved.

