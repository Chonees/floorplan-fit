---
type: decision
status: accepted
date: 2026-07-18
project: FloorplanFit
area: Loop 1 compression
---

# Retire coordinate-global pinch deformation

## Decision

Remove the current pinch deformation rule that shifts every supported point according to global X/Y marker thresholds. The replacement must be exact-path based and must never deform unrelated geometry merely because it lies beyond a marker coordinate.

## Preserved intent

- A marker is bound to its exact geometry path and position.
- The circle is the center of the removable capacity.
- Paired faces of one wall represent one synchronized reduction, not two additive reductions.
- Preview and DXF export must consume one shared deformation recipe and prove that non-target shapes are preserved.

## Pending choice

Resolved on 2026-07-19: use **closed-plan semantics**. Only the selected compressible span may change length. The connected downstream component may translate rigidly to close the removed span; its shapes, dimensions, angles, radii, wall thickness, and layer assignments must remain unchanged. Literal path-only gaps are rejected.

## Supersession

replaces: [[Inbox/2026-07-18 - Literal line-local pinch semantics]]

Related: [[Bugs/2026-07-18 - Pinch compression is coordinate-global instead of topology-local]], [[Inbox/2026-07-18 - Literal line-local pinch semantics]].
