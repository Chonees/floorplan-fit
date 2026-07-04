# 2026-06-30 - HousePlanSet projected dependent sheet export slice implemented

## Status
Implemented the first dependent-sheet artifact export slice for HousePlanSet.

## What changed
- Added `ExportProjectedPlanSheetHandler` as the Application use case for exporting a dependent sheet from an approved `SheetAdjustmentProjection`.
- Added `ExportProjectedPlanSheetRequest` / `ExportProjectedPlanSheetResponse` contracts.
- Added `IProjectedPlanSheetExporter` as the Application port.
- Added `ProjectedPlanSheetDxfExporter` in Infrastructure. It reads DXF pair streams and applies the approved projection transform to explicit coordinate point pairs before writing an output DXF.
- Wired the exporter and handler in Desktop DI.
- Added tests for the handler gate and basic DXF coordinate projection behavior.

## Boundary
- Only projections with `ReadyForExport` may produce artifacts automatically.
- Manual/low-confidence projections still block output until confirmation.
- The exporter currently transforms explicit DXF point pairs. Radius/block-internal scaling remains a later hardening point once real dependent sheets prove the needed entity coverage.
- This does not create an independent fit engine; it consumes the canonical floor-plan projection.

## Verification
- RED evidence: production files/classes did not exist when tests were added.
- `git diff --check` passed for touched tracked files; no build or dotnet test was run because repo rules forbid builds after changes.
