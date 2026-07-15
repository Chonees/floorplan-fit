---
type: current-state
created: 2026-07-04
status: active
---

# HousePlanSet sync - done vs missing

## Done
- FloorPlan is treated as canonical geometry owner for HousePlanSet projection.
- Canonical FloorPlan adjustment is recorded with placement JSON and AdjustmentRecipe JSON.
- Dependent sheets can be imported, registered, confirmed/rejected, unlinked, type-corrected, and listed in Desktop Library.
- Registered dependent sheets can receive a projection generated from the canonical placement/recipe.
- Multi-sheet package export writes canonical FloorPlan plus ready dependent projections from original dependent sheet sources.
- Export audit/manifest report sheet status, projection confidence, quality report, and recipe handling summary.
- Manual projection confirmation + re-export exists in SitePlanAdjustment UI, and the right sidebar scrolls.
- Projected Electrical DXF exporter preserves wiring/door arcs, projects dimension graphic blocks, and handles ELLIPSE vector 11/21 correctly.
- Runtime SEMINOLE TEST9 verified first canonical recipe route reached manifest with VerticalCompression operations.
- Implemented-uncompiled: initial export first persists the real top-level `X.dxf` scratch path. After package publication, the audit transaction updates the canonical adjustment to `X-plan-set/X-floorplan.dxf` in the same commit as export/audit persistence. Post-publication persistence failure explicitly rolls back workspace publication and the live SQLite transaction, then atomic publication removes the final package; the scratch file and previously committed scratch path remain. Successful completion deletes only explicitly owned scratch. Verification reads staged FloorPlan while audit/manifest artifacts use final paths. Manual re-export remains non-deleting and targets sibling `X-confirmed-plan-set`, never a nested package.

## Partial
- Roof/Facade projection handlers exist, but their semantics are simpler/rule-based and not proven at the same runtime level as Electrical.
- Dependent local compression recipe is recognized and reported, but still requires manual review instead of safe semantic deformation.
- Package export regenerates dependent sheets from original source path, but only for existing latest projections of the current canonical adjustment.
- Manual/missing dependent geometry remains a blocked/manual package state; valid ready technical outputs do not depend on an optional review comparison.

## Missing
- No explicit stale/NeedsReproject state exists yet.
- No automatic invalidation when a new FloorPlan canonical adjustment supersedes older dependent projections.
- No dedicated ReprojectDependentsFromCanonicalRecipe command exists yet.
- No dependent-local overrides model exists for electrical symbols/wires/annotations.
- No replay of dependent overrides after reprojection exists.
- No UI workflow yet showing stale -> reproject -> reviewed for dependents.
- No bidirectional sync by design: dependent sheet edits do not and should not mutate FloorPlan automatically.
- No `X-comparison.dxf` is produced until a source-preserving AC1032 merger can preserve the complete CAD object graph; the removed IxMilia reserialization path repeatedly produced AutoCAD black/invalid files.
