# HousePlanSet export audit sheet discovery implemented

## What
Extended Phase 7 export audit so it can discover dependent sheets from the PlanSet when no explicit projection ids are supplied.

## Why
A real HousePlanSet package audit cannot depend on the caller manually enumerating every projection. The system must see all dependent sheets in the package and make missing projections visible instead of silently omitting them.

## Current truth
- Commits:
  - `8ebfd25` (`docs: plan export audit sheet discovery`)
  - `cdcac8f` (`feat: discover sheets in export audit`)
  - `9073351` (`docs: record export audit sheet discovery`)
- `CreateMultiSheetExportAuditHandler` still supports explicit projection ids.
- If `DependentProjections` is empty, the handler reads dependent sheets via `IPlanSheetReader` and projections via `ISheetAdjustmentProjectionRepository.ListByPlanSetVersionAndCanonicalAdjustmentAsync(...)`.
- Sheets with a projection become `ProjectedAutomatically` or `RequiresManualConfirmation` based on projection status.
- Sheets without a projection become `MissingProjection` and count as manual-confirmation blockers.
- SQLite projection repository can now list projections by `plan_set_version_id` and `canonical_adjustment_id`.

## Boundaries
- No dependent-sheet DXF rewrite/export yet.
- No missing-projection generation.
- No automatic registration.
- No Desktop UI.
- No per-sheet fit engine.

## Verification
- Test-first RED evidence: `MissingProjection` enum value and projection-list repository method were absent before implementation.
- Scoped `git diff --check` and `git diff --cached --check` passed.
- No `dotnet test` and no `dotnet build` were run due repository rule.
