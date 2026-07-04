# 2026-06-30 - HousePlanSet package export auto exports ready projections

## Status
Improved package export so an empty dependent projection selection means: export every latest `ReadyForExport` dependent projection, then audit the entire dependent sheet set.

## What changed
- `ExportMultiSheetPlanSetPackageHandler` now resolves exportable projections automatically when `DependentProjectionIds` is empty.
- Auto mode uses the latest projection per dependent sheet and exports only those with `ReadyForExport` status.
- `CreateMultiSheetExportAuditRequest` now has `DiscoverAllDependentSheets` for package flows that need explicit exported paths plus global dependent-sheet audit coverage.
- `CreateMultiSheetExportAuditHandler` can now discover all dependent sheets while overlaying generated export paths for projections that were physically written.
- Tests cover auto-exporting ready projections, leaving manual/missing sheets in audit, and avoiding stale ready projections when a newer manual projection exists.

## Boundary
- Full Desktop UX remains pending.
- Auto mode still requires projections to already exist; it does not auto-register or auto-project sheets.
- No per-sheet fit engine was introduced.

## Verification
- RED evidence: prior package handler iterated only `DependentProjectionIds`, so empty selection exported nothing and audit paths stayed null.
- `git diff --check` passed for touched files; no `dotnet test` or `dotnet build` was run due repo rule.
