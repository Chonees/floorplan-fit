---
type: Implementation
date: 2026-05-20
project: floorplan-fit
status: implemented
replaces: Implementation/2026-05-20 - Bound dimensions use node axis span without normal drift.md
tags:
  - floorplan-fit
  - loop1
  - dimensions
  - measurement-bindings
  - articulation
---

# Bound dimensions use node movement deltas instead of absolute node anchors

## What changed

Manual-verified interval-bound dimensions no longer rebuild from absolute live node coordinates.

`DimensionIntervalReactiveProjector` now resolves both:

- the authored node point from the saved `MeasurementNodeDto` anchor fields;
- the live node point from adjusted preview geometry.

Then it calls `DimensionGeometryProjector.RebuildAssociatedDimensionFromAnchorDeltas`, which applies only each node's authored-to-live movement delta to the authored dimension endpoints.

## Product rule

The A/B node line is a measurement relationship. It is not the visual baseline of the cota.

Correct behavior:

- if both nodes move equally, the dimension translates equally;
- if one endpoint moves toward the other, the dimension shrinks by that delta;
- if the node line lives somewhere else in the plan, the dimension does not jump there;
- the authored dimension angle/side/height stays stable.

## Follow-up: visual axis correction

Visual QA showed that delta-based anchoring was still not enough for real CAD dimensions whose definition points are not parallel to the visible cota line.

`DimensionGeometryProjector.ResolveAxis` now prefers the visible cota axis:

1. farthest terminal insert pair;
2. longest drawn dimension primitive;
3. definition-point vector;
4. angle fallback.

This prevents the reactive transform from making a visually horizontal cota become diagonal just because `DefPoint -> DefPoint2` is diagonal.

## Verification

- RED: `Project_applies_live_node_delta_instead_of_anchoring_dimension_to_absolute_node_coordinates` failed because the dimension jumped from authored X=100 to node X=300.
- GREEN: focused `DimensionIntervalReactiveProjectorTests` passed 10/10.
- Application focused slice passed 23/23.
- Desktop focused preview/review slice passed 86/86.
- RED follow-up: `Project_preserves_visual_dimension_axis_when_definition_points_are_not_parallel_to_the_cota_line` failed because a horizontal cota line changed to a sloped transformed line.
- GREEN follow-up: `DimensionIntervalReactiveProjectorTests` passed 11/11, Application focused slice passed 24/24, Desktop focused preview/review slice passed 86/86.
