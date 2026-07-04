---
type: Implementation
date: 2026-05-20
project: floorplan-fit
status: superseded
replaces: Bugs/2026-05-20 - Bound dimensions deform because live nodes drive visual transform.md
replaced_by: Implementation/2026-05-20 - Bound dimensions use node movement deltas instead of absolute node anchors.md
tags:
  - floorplan-fit
  - loop1
  - dimensions
  - measurement-bindings
  - articulation
---

# Bound dimensions use node axis span without normal drift

> [!WARNING] Superseded
> This fixed normal drift but still treated live node coordinates too literally. The corrected model is delta-based: authored node point -> live node point movement is applied to the authored dimension, instead of anchoring the dimension to absolute node coordinates.

## What changed

Reactive bound dimensions still read live measurement node positions during active articulation, but the visual rebuild now ignores the nodes' perpendicular/normal offset.

In `DimensionGeometryProjector.ResolveStylePreservingLinearTransform`, the style-preserving transform keeps `NormalDelta = 0`. The live A/B nodes therefore affect only the authored-axis span (`U`), not the dimension's authored side/baseline (`V`).

## Why

The previous implementation treated the live A/B node segment too literally. If the start node lived on a wall below the authored dimension line, the whole cota was pulled down toward that wall and could visually deform during articulation.

The intended model is:

- nodes define what interval/span is measured;
- the live adjusted geometry says how that span changes during pinch;
- the CAD cota keeps its authored orientation, side, and normal offset.

## Verification

- RED: `Project_uses_live_node_axis_span_without_pulling_the_dimension_to_the_node_normal_position` failed because `DefPointY` became `220` instead of the authored `100`.
- GREEN: focused Application slice passed 22/22.
- Desktop preview/review focused slice passed 86/86.
