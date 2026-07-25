---
type: decision
status: superseded
date: 2026-07-19
project: FloorplanFit
area: Loop 1 compression and Loop 2 Electrical projection
replaces: "[[2026-07-19 - Pinches as CAD-style stretch actions]]"
replaced_by: "[[2026-07-20 - Replace pinches with parametric adaptation profiles]]"
---

# Adopt CAD-style pinch stretch actions

> [!warning] Superseded after runtime rejection
> The user rejected both the coordinate-global V1 behavior and the connected CAD-style V2 behavior in live use. The replacement direction is documented in [[2026-07-20 - Replace pinches with parametric adaptation profiles]].

## Decision

Replace coordinate-global pinch deformation with one versioned, entity/vertex-aware CAD-style stretch action per logical paired-wall pinch group.

The two markers already authored on both faces of a wall represent one total reduction. Only those exact marked spans may shorten. Complete geometry on the closing side may translate rigidly once; fixed and unrelated geometry must remain coordinate-identical. Wall thickness and all supported entity shapes must be preserved. Unsupported or unmarked crossings fail closed.

## Compatibility boundary

Keep the existing canonical FloorPlan adjustment, confirmed/source-bound Electrical registration, projection states, HousePlanSet package export, manifest, and audit pipeline. New adjustments write recipe `v2` into the existing recipe JSON; historical `v1` remains readable. Preview, canonical FloorPlan DXF, and Electrical DXF must replay the same pure deformation engine.

No whole-building topology graph and no new SQLite table are part of the minimum accepted design.

## Implementation gate

Write and review the technical design and finite TDD plan before changing the deformation engine. Repository policy forbids agent-run `.NET` build/test/restore/watch/Desktop commands; completion ends at a statically verified external runtime handoff.

## Origin

- Accepted proposal: [[2026-07-19 - Pinches as CAD-style stretch actions]].
- Retired model: [[2026-07-18 - Retire coordinate-global pinch deformation]].
- Finite execution goal: [[2026-07-19 - Finite goal for CAD-style pinch deformation]].
