# HousePlanSet design spec updated to current export loop

Date: 2026-07-01
Type: Implementation
Status: Current

## What changed
- Updated `docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md` to reflect the current as-built HousePlanSet loop.
- The spec now states that Desktop exposes the minimal visible loop: select canonical floor-plan version, import dependent sheets, unlink wrong imports, register/confirm sheets, confirm manual projections, and see package export audit lines after canonical adjustment.
- The spec now records that `ExportProjectedPlanSheetHandler` and `ProjectedPlanSheetDxfExporter` export `ReadyForExport` dependent DXFs by applying stored projection transforms.
- Phase status now reflects dependent DXF projection export, projection discovery, missing/manual audit, and quality metadata.

## Why
The architecture document was stale: it still described Phase 7 as not rewriting dependent DXFs and suggested restarting at Phase 1. That contradicted the current implementation and would mislead future work.

## Boundary
- This was a documentation/current-truth correction, not a new runtime feature.
- Post-review re-export UX and richer manual geometry review remain future improvements.

## Verification
- Inspected `ExportProjectedPlanSheetHandler`, `ProjectedPlanSheetDxfExporter`, Desktop export flow, and related tests before updating the spec.
- Ran scoped `git diff --check` for the design spec.

## Related
- [[2026-07-01 - Desktop confirms dependent sheet projection]]
- [[2026-07-01 - Desktop registers roof and facade sheets from library]]
