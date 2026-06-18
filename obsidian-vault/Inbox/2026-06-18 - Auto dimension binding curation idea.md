# 2026-06-18 - Auto dimension binding curation idea

## Type
Product idea / Inbox

## Product loop
Loop 1 Edit / Review curation.

## User idea
Use already curated floor plans to make the app automatically place measurement nodes and relate dimensions/cotas to those nodes. The first automation target should be dimension-node relation, because the plans share patterns in how dimensions are authored and curated.

## Verified current data/code context
- The app already has the core data model for this: `MeasurementCorridor`, `MeasurementNode`, and `DimensionIntervalBinding`.
- `DimensionIntervalReactiveProjector` and Loop 2 auto-fit already depend on these bindings, so automating them would directly reduce manual curation and improve downstream fit behavior.
- Local workspace evidence from `workspace/app.db` on 2026-06-18:
  - `floorplan_curations`: 13
  - `floorplan_dimension_interval_bindings`: 1689
  - `measurement_corridors`: 1784
  - `measurement_nodes`: 3535
  - Independent templates with meaningful bindings appear limited: Santa Barbara has 16 bindings; Seminole versions have up to 159 bindings but are repeated versions of the same plan.

## Initial product direction
Prefer a geometry-first candidate proposer before any true model training:
1. infer likely measurement corridors/nodes from extracted dimension primitives and wall/candidate geometry,
2. score candidate dimension-to-node bindings by axis, proximity, and measured value agreement,
3. present suggestions for human approval,
4. only auto-apply very high-confidence bindings after enough reviewed history exists.

## Open question
Decide whether the first feature should be a safe **Auto-relacionar cotas** suggestion button for the current plan, rather than full AI auto-curation.
