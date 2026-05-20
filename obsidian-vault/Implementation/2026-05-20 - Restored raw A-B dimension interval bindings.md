---
date: 2026-05-20
type: implementation
status: superseded
replaced_by: Implementation/2026-05-20 - Bound dimensions anchor to live nodes without visual deformation.md
replaces:
  - Bugs/2026-05-19 - Corridor interval math is axis-aligned not angle-aware.md
  - Implementation/2026-05-18 - Axis-projected interval bindings fix cross-wall width and height spans.md
  - Decisions/2026-05-18 - Diagonal measurements are not equivalent to axis intervals.md
---

# Restored raw A-B dimension interval bindings

## What changed
Manual-verified dimension interval bindings now use the real two-node A/B line again when rebuilding reactive dimensions.

## Why
The intended curation workflow is: place exactly two nodes A and B on valid floor-plan geometry, then bind any selected dimension to those nodes. The previous axis-only guard/projection made the system collapse cases where both nodes shared the same X/Y axis coordinate and blocked free-angle dimensions.

## Current behavior
- `DimensionIntervalReactiveProjector` resolves each node's live world point from `GeometryPathId + PositionRatio`.
- It rebuilds the associated dimension from those raw start/end points.
- It no longer rejects `FreeAngle` dimensions.
- It no longer requires the dimension axis to match the corridor axis.
- The preview measurement overlay now draws the raw A/B segment, not a projected horizontal/vertical baseline.

## Important nuance
`MeasurementNode.AxisCoordinate` is still stored for corridor/pinch band overlap semantics, so two points on the same vertical wall under a `Width` corridor can still display the same axis coordinate. That is not data loss: the actual A/B geometry comes from `AnchorX`, `AnchorY`, and especially `PositionRatio` resolved on the geometry path.

## Verification
- RED: added/updated tests that failed with the axis-only behavior:
  - same vertical wall with matching width-axis coordinates must still rebuild from A/B
  - cross-wall width/height bindings must use raw A/B
  - free-angle dimensions must rebuild from the manual two-node line
  - free-angle dimensions must be saveable in Review VM
  - overlay must draw raw A/B
- GREEN:
  - `DimensionIntervalReactiveProjectorTests`: 6/6 passed
  - `MeasurementBindingFloorPlanReviewViewModelTests`: 14/14 passed
  - `MeasurementBindingPreviewLayerRendererTests`: 4/4 passed
  - preview regression slice: 68/68 passed


## Superseded
This note's core persistence truth remains useful: measurement nodes are still resolved from live geometry and free-angle dimensions can still be bound. However, the later implementation replaces the raw visual A/B rebuild with a style-preserving projection on the authored dimension axis so CAD dimensions do not deform during articulation.
