# 2026-06-30 - Export audit includes DataCollection quality report

## Type
Implementation

## What
`CreateMultiSheetExportAuditHandler` now attaches the DataCollection `PlanSetQualityReportDto` to the multi-sheet export audit response and package manifest when the quality-report handler is available.

## Why
HousePlanSet export needs to show not only which sheets were exported/projected/missing, but also the quality of sheet registration/projection that produced the package.

## Where
- `src/FloorplanFit.Contracts/PlanSets/MultiSheetExportAuditDto.cs`
- `src/FloorplanFit.Application/PlanSets/ExportAudit/CreateMultiSheetExportAuditHandler.cs`
- `tests/FloorplanFit.Application.Tests/PlanSets/ExportAudit/CreateMultiSheetExportAuditHandlerTests.cs`

## Boundary
This is export metadata only. It does not create a dashboard, does not block package export when quality reporting fails, and does not introduce a per-sheet fit engine.

## Verification
- RED evidence: the export-audit test asserted `response.QualityReport` and `manifestAudit.QualityReport` while production had no `QualityReport` contract/handler wiring.
- GREEN evidence: production now passes the quality report into both the draft manifest audit and final response DTO.
- Static check: `git diff --check` passed for the touched files.
- No `dotnet test` / no `dotnet build`, per repo rule.
