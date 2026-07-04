---
type: bug
date: 2026-06-09
topic: loop2-dimension-anchor-coordinate-space
---
# Auto-fit dimensions used unprojected node anchors

## Symptom
During Loop 2 Adjust to Site Plan auto-fit, some related dimensions could visually detach from the wall endpoints they were supposed to measure. The dimension looked like it was left in the air instead of staying elastically anchored to the reduced walls.

## Root cause
`DimensionIntervalReactiveProjector` received projected/source geometry for Loop 2, but still used raw `MeasurementNodeDto.AnchorX/Y` as the authored anchor points.

In Loop 2, geometry and dimensions are already translated/scaled into site-plan preview coordinates, while measurement nodes come from the original curated floor-plan coordinate space. Comparing live projected wall points against raw unprojected node anchors produced huge false deltas, which could push dimension endpoints away from the walls.

## Fix
- When `sourceGeometry` is supplied, authored dimension anchors are now resolved from that source geometry using the node's `GeometryPathId` and `PositionRatio`.
- Node offsets are still applied, but in the same coordinate space as the preview/source geometry.
- If source geometry is missing a path, the projector falls back to the original raw-node behavior for compatibility.

## Verification
- RED/GREEN test: projected source geometry at `1000..1124` with unprojected nodes at `100..224` previously moved the dimension to `1900`; now it stays anchored at `1000..1122` after a 2-inch right-side reduction.
- Focused Application projector/auto-fit tests passed 23/23.
- Focused Desktop site-plan adjustment tests passed 15/15.
- `git diff --check` passed with LF-to-CRLF warnings only.

## Product impact
Loop 2 auto-fit dimensions now preserve their elastic wall anchoring in the site-plan preview coordinate space: if a wall endpoint moves, the dimension endpoint follows that wall instead of using stale original-floor coordinates.
