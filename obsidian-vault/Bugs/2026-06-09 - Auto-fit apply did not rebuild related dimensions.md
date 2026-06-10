---
type: bug
date: 2026-06-09
topic: loop2-apply-dimensions-not-reactive
---
# Auto-fit apply did not rebuild related dimensions

## Symptom
When applying a selected auto-fit option in Adjust to Site Plan, geometry compressed but related dimensions did not re-accommodate like they do when interacting with preview edit handlers.

## Root cause
`SitePlanAdjustmentViewModel.ApplyAutoFitPlan` compressed geometry and then transformed every `DimensionDto` point-by-point through `TransformDimension`. That moved dimension graphics but did not recalculate associated interval measurements, display text, primitives, or style-preserving dimension layout.

The edit preview path uses `DimensionIntervalReactiveProjector.Project(...)` with measurement corridors, nodes, interval bindings, articulation bands, active pinch group id, and source geometry.

## Fix
`SitePlanAdjustmentViewModel` now receives the same reactive context from `FloorPlanReviewViewModel`:
- measurement corridors
- measurement nodes
- dimension interval bindings
- articulation bands

On Apply, each selected auto-fit step:
1. stores source geometry before compression,
2. compresses the selected geometry,
3. reprojects dimensions with `DimensionIntervalReactiveProjector.Project(...)` for the selected pinch group,
4. falls back to point transform only when reactive context is missing.

## Verification
- RED: desktop test proved a 124" bound width dimension stayed semantically stale after a 2" right-side reduction.
- GREEN: after fix, the same apply produces 122" / `10'-2"` with `DisplayTextSource = ReactiveAssociatedMeasurement`.
- Desktop focused tests passed 19/19.
- Application focused auto-fit + reactive projector tests passed 22/22.
- Infrastructure OpenAI/Claude focused tests passed 6/6.
- `git diff --check` passed with LF-to-CRLF warnings only.
