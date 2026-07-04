# 2026-06-30 - HousePlanSet package export derives dependent sheet source paths

## Status
Improved package export orchestration so callers no longer need to pass source/output paths per dependent sheet.

## What changed
- `ExportMultiSheetPlanSetPackageRequest` now takes a `PackageDirectory` and dependent projection ids.
- `ExportMultiSheetPlanSetPackageHandler` loads each projection, resolves its dependent sheet source path, generates the dependent output path, exports the projected sheet, then audits/manifests the package with the generated path.
- Added `PlanSheetSourceDto` and `IPlanSheetSourceReader`.
- `SqlitePlanSheetRepository` now implements `IPlanSheetSourceReader` by joining `plan_sheets` to `imported_documents` and returning the managed DXF source path.
- Desktop DI now wires `IPlanSheetSourceReader` to the existing `SqlitePlanSheetRepository`.
- Added tests for package source/output derivation and plan-sheet source persistence.

## Boundary
- The package export still needs explicit projection ids from the caller.
- Full Desktop UX remains pending.
- Discovery mode in audit still reports missing/manual state, but physical export is intentionally explicit until the UI selection path exists.
- No per-sheet fit engine was introduced.

## Verification
- RED evidence: `IPlanSheetSourceReader` and `PlanSheetSourceDto` did not exist, and the package request had no `PackageDirectory` / projection-id API before the test change.
- `git diff --check` passed for touched files; no `dotnet test` or `dotnet build` was run due repo rule.
