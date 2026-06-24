# 2026-06-22 - Measurement corridors are two-node spans

## Type
Decision / Product rule

## Context
In Loop 1 floor-plan curation, a measurement corridor/franja represents one measurable interval, not an arbitrary collection of points.

## Verified current state
- The active edit flow stores franjas as `MeasurementCorridor` and points as `MeasurementNode`.
- `AddMeasurementNodeHandler` currently counts existing nodes in the corridor and assigns `SortOrder = existing.Count + 1`.
- No source-level guard currently prevents a third node in the same franja.
- The Desktop UI already labels node counts per franja (`0 nodos`, `1 nodo`, `2 nodos`), but it does not make `2` an invariant yet.

## Decision
A floor-plan measurement franja must have at most two nodes.

## Why
A franja models a start/end interval. More than two nodes changes the concept from an interval into a polyline/list of arbitrary points, which makes dimension binding ambiguous.

## Implementation direction
- Enforce the invariant in Application, preferably in `AddMeasurementNodeHandler`, so UI and persistence callers cannot bypass it.
- Add a tiny Desktop guard/message so the user gets immediate feedback instead of discovering the rule through a lower-level exception.
- Add focused tests for the third-node rejection path.

