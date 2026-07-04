# 2026-07-02 - Canonical adjustment recipe slice 1 plan

type: Implementation
status: Planned, not implemented

## What
Created the concrete Phase 3 implementation plan for the first canonical AdjustmentRecipe slice.

## Why
The goal requires a small approved slice before implementation: FloorPlan adjustment should expose a recipe, dependent projection should consume recipe information, and export/audit should say what applied versus what requires review.

## Plan artifact
- `docs/superpowers/plans/2026-07-02-canonical-adjustment-recipe-slice1.md`

## Slice boundary
This slice reuses existing `AdjustedSitePlanPlacementDto` / `placement_json` as the recipe payload. It adds explicit recipe summary/reporting around existing compression steps, but does not locally deform ElectricalPlan/Roof/Facade DXF geometry yet.

## No-go decisions
- No new recipe table yet.
- No full electrical rerouting.
- No unknown DXF curve deformation.
- No independent fit engine for dependent sheets.
- No FloorPlan exporter rewrite.

## Key Learnings:
1. The first implementable AdjustmentRecipe slice can be reporting-first: derive recipe summary from existing placement JSON instead of adding new persistence.
2. Dependent projection can satisfy the first demonstrable route by consuming recipe metadata and reporting local compression as review-required before doing dangerous DXF deformation.
3. The safest next code change is small: contract summary, projection summary, persistence column, audit/UI propagation, and focused tests.
