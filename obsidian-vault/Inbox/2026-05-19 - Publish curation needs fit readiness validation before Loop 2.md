# Publish curation needs fit readiness validation before Loop 2

## What
Current `PublishFloorPlanCurationHandler` only verifies that the draft curation exists, is still Draft, and has at least one pinch marker before setting the curation to Published and making it the template active published curation.

## Why it matters
This is not a strong enough contract for the future site-plan auto-fit system. The published curation data is already structurally rich, but publish does not yet prove that the data is complete, coherent, or fit-ready.

## Current storage truth
- `floorplan_curations` stores status/version/published timestamp.
- `floorplan_templates.active_published_curation_id` points to the active published curation.
- Pinch data lives in `pinch_groups` and `pinch_markers`.
- Measurement semantics live in `measurement_corridors`, `measurement_nodes`, and `floorplan_dimension_interval_bindings`.
- Curated classifications/positions/dimensions live in override tables keyed by `floorplan_curation_id`.
- Extracted CAD artifacts remain in extraction-run tables and are resolved with curation overlays/read-model projection.

## Risk
If publish remains only a status flip plus one-pinch-marker guard, Loop 2 can receive a published floor plan that is visually curated but not semantically complete enough for deterministic fitting.

## Proposed direction
Add a Fit Readiness / Published Curation Contract validation before publish. It should produce a report with CRITICAL/WARNING/SUGGESTION issues across geometry, pinches, protected artifacts, measurement bindings, dimensions, and future site-fit needs.

## Follow-up verification: measurement and pinch persistence
Verified code and focused tests on 2026-05-19:

- `AddMeasurementCorridorHandler` persists corridor name, axis, guide geometry path, band min/max, status `Verified`, and sort order.
- `AddMeasurementNodeHandler` persists node corridor membership, source artifact kind/id, geometry path, snap kind, anchor X/Y, axis coordinate, offsets, and position ratio.
- `SaveDimensionIntervalBindingHandler` persists dimension id + corridor id + start/end node ids + status `ManualVerified` + interval start/end coordinates copied from the nodes' `AxisCoordinate` values.
- `AddPinchGroupHandler` persists group name, axis, and sort order.
- `AddPinchMarkerHandler` persists source wall candidate id, geometry path id, group id, position ratio, max trim, and sort order.
- Wall candidates are accepted by default in the curation/read-model flow unless rejected; `RejectWallCandidateHandler` removes any pinch marker tied to a rejected source candidate.

Fresh focused verification passed:
- Application tests: 7/7 pass for pinch group/marker, interval binding cleanup, publish, and reject-wall behavior.
- Infrastructure tests: 2/2 pass for SQLite round-trip of corridors, nodes, and manual interval bindings.

Conclusion: the raw information the user described is being saved correctly. The remaining risk is not persistence shape; it is publish readiness/quality validation before future Loop 2 consumes the data.

## 2026-05-21 verification update
Re-checked the current code before answering the publish question:

- `PublishFloorPlanCurationHandler` still only checks: template exists, curation exists, curation is Draft, and the curation has at least one pinch marker. Then it calls `curation.Publish(...)`, `template.SetActivePublishedCuration(curation.Id)`, updates both rows, and saves.
- `SaveDimensionIntervalBindingHandler` persists a manual cota relation only when the operator saves it: dimension id, corridor id, start node id, end node id, `ManualVerified`, and interval coordinates from the two nodes.
- `SqliteFloorPlanReviewSessionReader` reads `pinch_groups`, `pinch_markers`, `measurement_corridors`, `measurement_nodes`, and `floorplan_dimension_interval_bindings` for the active curation id and exposes them in `FloorPlanReviewSessionDto`.
- `DimensionIntervalReactiveProjector` consumes only `ManualVerified` interval bindings, resolves their start/end nodes against adjusted preview geometry, and adjusts the affected dimensions during pinch preview when the corridor axis matches the active articulation band.
- The storage shape is still good enough for the intended pinches + A/B franja + cota relation model, but publish still does not certify completeness or quality.

Extra caveat found: `StartOrResumeCurationHandler` creates a new draft with `basedOnCurationId = published?.Id`, but measurement corridors/nodes/bindings are currently loaded only from the active draft id, not from lineage. If a future flow expects published Fit relationships to be inherited into a new draft, add explicit copy/inheritance for Fit semantic tables.
