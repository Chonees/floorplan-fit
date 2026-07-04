# 2026-06-30 - Desktop shows HousePlanSet quality report summary

## Type
Implementation

## What
Desktop now adds a compact `Quality:` line to `PlanSetExportAuditLines` when the multi-sheet package audit includes a `PlanSetQualityReportDto`.

## Why
The HousePlanSet tool should make registration/projection confidence visible to the user after package export, not hide it only inside the manifest.

## Where
- `src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/SitePlanAdjustmentPreviewProjectorTests.cs`

## Boundary
This is a minimal summary line, not a dashboard or review workflow. It reads existing export audit metadata and does not introduce a new service or per-sheet fit engine.

## Verification
- RED evidence: Desktop test expected a `Quality` audit line with registration/projection counts and confidence percentages while production had no `QualityReport` rendering.
- GREEN evidence: `BuildPlanSetExportAuditLines(...)` now appends the quality summary when `audit.QualityReport` exists.
- Static check: `git diff --check` passed for the touched Desktop files.
- No `dotnet test` / no `dotnet build`, per repo rule.
