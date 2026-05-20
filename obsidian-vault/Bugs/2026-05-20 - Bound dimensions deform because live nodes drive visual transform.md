---
type: Bug
date: 2026-05-20
project: floorplan-fit
status: fixed
replaces: Implementation/2026-05-20 - Bound dimensions anchor to live nodes without visual deformation.md
replaced_by: Implementation/2026-05-20 - Bound dimensions use node axis span without normal drift.md
tags:
  - floorplan-fit
  - loop1
  - dimensions
  - measurement-bindings
  - articulation
---

# Bound dimensions deform because live nodes drive the visual transform

## Verified root cause

During active pinch preview, the app builds adjusted preview geometry and then resolves bound measurement nodes against that adjusted geometry. `DimensionIntervalReactiveProjector` passes those live node points into `DimensionGeometryProjector.RebuildAssociatedDimension`.

`RebuildAssociatedDimension` currently treats the live A/B node span as the new visual span of the CAD dimension. It applies a local-axis transform to definition points, line primitives, text, inserts, circles, arcs, and solids. That is why dimensions can slide down toward wall nodes, skew visually, or compress their full primitive layout instead of preserving the authored cota shape.

## Correct product rule

The A/B node line is not the visual geometry of the cota. It is curation metadata that says what interval/span this dimension represents and how much of that interval is affected by a pinch/articulation band.

The dimension should:

- keep its authored visual position, side, offset, text grammar, and primitive topology;
- use corridor/nodes/band metadata only to compute whether the measured span is affected and by how much;
- reduce/update only the affected measurement span/value, not drag the whole CAD dimension to the live node segment.

## Why the previous tests were insufficient

The latest passing tests asserted that bound dimensions follow live nodes and that every primitive is transformed consistently. Those tests caught internal consistency of the wrong model, but they did not encode the desired UX: authored dimension geometry must remain stable while the measurement contract changes.

## Next implementation direction

Replace the live-node visual anchoring tests with tests that assert:

1. bound dimensions preserve authored geometry/normal offset/side during articulation;
2. A/B nodes determine measured interval and trim impact only;
3. only dimensions whose measured interval intersects the active band get their measured value/span reduced;
4. dimensions whose anchors translate together but whose interval is not reduced keep the same authored display geometry and value.

## Fixed

Implemented a narrower correction in `DimensionGeometryProjector`: live nodes still determine the dimension's axis span, but their perpendicular/normal offset no longer translates the CAD dimension. This keeps cotas on their authored side/baseline while letting the span reduce along the authored axis.
