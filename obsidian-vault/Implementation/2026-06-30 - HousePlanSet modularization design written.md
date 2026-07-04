---
type: implementation
date: 2026-06-30
topic: architecture/house-plan-set-modularization
---
# 2026-06-30 - HousePlanSet modularization design written

## What
Created the House Plan Set modularization design spec at `docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md`.

## Why
The product direction is to structure Floorplan Fit as a tool for full house plan sets, not only isolated floor plans. The existing floor-plan curation/adjustment work remains the canonical engine, while electrical, roof, and facade/elevation sheets become dependent sheets registered against that canonical model.

## Design summary
- Keep the current layered modular monolith; do not split new `.csproj` projects yet.
- Add a thin PlanSet backbone first: `HousePlanSet`, `PlanSetVersion`, `PlanSheet`.
- Preserve published floor-plan curation as the only source of canonical adjustment.
- Register dependent sheets via `SheetRegistration` with confidence, anchor evidence, warnings, and manual confirmation.
- Project the approved canonical adjustment to dependent sheets using `SheetAdjustmentProjection`.
- Export coherent packages via `MultiSheetExportAudit`.
- Collect quality evidence through `DataCollection`.

## Modules defined
1. HousePlanSet / PlanSetVersion
2. SheetImport
3. SheetClassification
4. SheetRegistration
5. CanonicalFloorPlanAdjustment
6. SheetAdjustmentProjection
7. MultiSheetExportAudit
8. DataCollection

## Phases defined
1. Conceptual PlanSet Backbone
2. Multiple Sheets per House
3. Electrical Registration
4. Project Canonical Adjustment to Electrical
5. Roof Registration and Projection
6. Facade/Elevation Analysis
7. Multi-Sheet Export with Audit

## Verification
- Self-check found all eight requested modules and all seven requested phases.
- Placeholder scan found no `TBD`, `TODO`, `PLACEHOLDER`, `???`, or leftover template phase placeholder.
- No build was run. Commit: `7079de0` (`docs: design house plan set modularization`).
