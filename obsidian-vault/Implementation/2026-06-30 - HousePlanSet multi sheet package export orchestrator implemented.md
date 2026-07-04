# 2026-06-30 - HousePlanSet multi sheet package export orchestrator implemented

## Status
Implemented the first package-level orchestration slice for HousePlanSet export.

## What changed
- Added `ExportMultiSheetPlanSetPackageHandler`.
- Added `ExportMultiSheetPlanSetPackageRequest`.
- The handler exports each dependent `ReadyForExport` projection through `ExportProjectedPlanSheetHandler`.
- After physical dependent artifacts are written, it calls `CreateMultiSheetExportAuditHandler` with the generated output paths.
- The resulting audit/manifest now receives real dependent sheet artifact paths for explicitly requested package exports.
- Desktop DI now wires the package export handler.
- Added an Application test proving dependent sheet artifacts are exported before the package audit persists their paths.

## Boundary
- This is explicit package orchestration only: the caller must provide dependent projection id, source path, and output path.
- Discovery mode remains audit-only for sheets without explicit artifact requests.
- No per-sheet fit engine was introduced.
- Full Desktop UX still remains pending.

## Verification
- RED evidence: `ExportMultiSheetPlanSetPackageHandler` and `ExportMultiSheetPlanSetPackageRequest` did not exist when the test was added.
- `git diff --check` passed for touched tracked files; no `dotnet test` or `dotnet build` was run due repo rule.
