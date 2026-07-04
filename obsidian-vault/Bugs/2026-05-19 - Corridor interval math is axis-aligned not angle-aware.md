> Superseded on 2026-05-20. Replaced by: $replacement. The active dimension interval rebuild path now uses the raw A/B node line; corridor/pinch band overlap remains axis-tagged.\n\n# Corridor interval math is axis-aligned, not angle-aware

## What
The current measurement corridor / dimension interval projection supports only global `Width` and `Height` axes. It does not store a corridor angle/vector and does not project/rebuild dimensions along an arbitrary inclined corridor direction.

## Evidence
- `DimensionAxisClassifier` classifies dimensions as `Width` when `dy ~= 0`, `Height` when `dx ~= 0`, otherwise `FreeAngle`.
- `DimensionIntervalReactiveProjector` refuses `FreeAngle` dimensions and returns them authored/static.
- `ProjectIntervalToCorridorAxis(...)` rebuilds Width intervals by changing X while preserving the dimension Y, and Height intervals by changing Y while preserving the dimension X.
- `ArticulationBandProjector` resolves pinch band coordinates from global X for Width and global Y for Height.
- `MeasurementCorridorDto` stores axis tag and guide geometry path, but no angle, unit vector, origin, or local coordinate frame.

## Why it matters
If a future site-plan fit needs to articulate along an inclined/rotated corridor, the current math will not reduce the dimension proportionally along that corridor's true angle. It is safe only for axis-aligned Width/Height intervals in the floor-plan coordinate system.

## Direction
Add an angle-aware/local-coordinate corridor model before claiming support for inclined corridors:
- corridor origin
- unit axis vector
- normal vector
- scalar start/end coordinates projected onto that axis
- reverse projection back to world coordinates
- tests for an inclined corridor where reduction along the vector changes both X and Y proportionally

