---
type: inbox
status: superseded
date: 2026-07-19
project: FloorplanFit
area: Loop 1 compression
replaced_by: "[[2026-07-19 - Pinches as CAD-style stretch actions]]"
---

# Closed pinch scope needs explicit confirmation

> Superseded by [[2026-07-19 - Pinches as CAD-style stretch actions]]. The researched CAD-style selection-set model provides a smaller deterministic scope than inferring and confirming a whole downstream component.

## Verified model gap

Current geometry stores ordered paths and segments, but no cross-path junction identity, adjacency graph, host-wall relationship, paired-wall identity, or downstream-component membership. The `v1` recipe stores only axis, edge, coordinate, and delta, so it cannot replay CERRADO semantics.

## Options

1. **Automatic inference only:** derive components from endpoint/intersection tolerances. This is compact but unreliable around doors, gaps, near misses, and ambiguous parallel walls.
2. **Explicit manual scope only:** user selects every moving path/entity. Reliable but tedious.
3. **Auto-propose plus one visual confirmation (recommended):** a resolver proposes the two synchronized wall faces and the rigidly moving component; the preview colors fixed, compressible, and moving geometry; the user confirms or corrects it once. Persist the exact scope in a versioned `v2` recipe so preview, FloorPlan DXF, and Electrical consume the same decision.

## Minimal architecture

- Reuse `PinchGroupId` as one logical reduction; member markers no longer add independent deltas.
- Group capacity is constrained by the weakest synchronized member rather than summed.
- Add only operation-scoped membership; do not persist a speculative whole-building topology graph.
- Keep historical `v1` recipes readable; new confirmed operations write `v2`.
- One pure deformation engine owns local span compression and rigid component translation.
- Ambiguous or unconfirmed scope fails closed and produces no partial export.

## Required invariant

Only marked bridge spans change length. Every moving entity receives one identical translation vector. Every fixed/unrelated entity is coordinate-identical. Paired wall thickness is preserved and requested width/height reduction is achieved exactly.
