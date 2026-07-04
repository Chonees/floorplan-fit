# 2026-06-30 - HousePlanSet remaining work after package manifest

## Status
Phase 7 now writes a package manifest, but the HousePlanSet goal is not complete yet.

## Verified evidence
- Latest commits include `72ab90a feat: write export package manifest` and `ae9aa60 docs: record export package manifest bridge`.
- `PlanSetExportManifestWriter` writes `<workspace>/exports/plan-sets/<exportId>/manifest.json`.
- `CreateMultiSheetExportAuditHandler` audits canonical and dependent sheets, discovers dependent sheets when `DependentProjections` is empty, and marks missing dependent projections as `MissingProjection`.
- Existing DXF exporter surface is still floor-plan/adjusted-site-plan oriented; there is no first-class dependent sheet DXF rewrite/copy/package exporter yet.

## What remains
1. Create the dependent-sheet export/copy/rewrite stage that consumes approved projections and outputs actual electrical/roof/facade artifacts.
2. Add the package orchestration/UI entry point after canonical floor-plan adjustment/export.
3. Run an end-to-end real scenario: floor plan + electrical/roof/facade, adjust once, project dependents, audit, manifest, exported artifacts.
4. Add DataCollection read/reporting for repeated blockers such as missing projection, low confidence, manual confirmation, and sheet-specific warnings.
5. Harden the bridge: real `PlanSetVersion` records instead of using `FloorPlanVersion.Id` as the temporary plan-set version id.
6. Improve classification/override UX and package re-open/read surfaces.

## Architectural boundary
No extra fit engine should be added for electrical/roof/facade. The missing work is orchestration and artifact generation from existing projections, not a second adjustment authority.
