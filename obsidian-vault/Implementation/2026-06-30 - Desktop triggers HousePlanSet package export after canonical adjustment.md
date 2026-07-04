# Desktop triggers HousePlanSet package export after canonical adjustment

## What changed
- Connected the Desktop Adjust-to-Site-Plan export flow to `ExportMultiSheetPlanSetPackageHandler`.
- After the combined adjusted DXF is exported and `CanonicalFloorPlanAdjustment` is recorded, `SitePlanAdjustmentViewModel` now exports/audits the HousePlanSet package using the recorded canonical adjustment id.
- `SitePlanAdjustmentViewModel.LastPlanSetExportAudit` exposes the resulting package audit to the UI layer.
- `LibraryViewModel` resolves and passes `ExportMultiSheetPlanSetPackageHandler` into the adjustment view model.

## Why
The app goal is HousePlanSet tooling, not isolated floor-plan export. The floor plan remains the canonical adjustment source, but Desktop now has the first real bridge from that canonical adjustment into dependent sheet package export/audit.

## Boundary
- No new per-sheet fit engine was introduced.
- Package export uses automatic dependent projection discovery by passing an empty projection id list.
- Manual/missing dependent sheets remain visible through the audit instead of being silently forced.
- No full UI review surface was added yet; only the export flow now captures the audit result.

## Verification
- Added test-first coverage in `SitePlanAdjustmentPreviewProjectorTests`: Desktop export records the canonical adjustment, exports a ready electrical projection through package export, and exposes `LastPlanSetExportAudit`.
- RED was verified statically because production initially had no `planSetPackageExporter` / `LastPlanSetExportAudit` API.
- `git diff --check` passed for the touched Desktop/test files.
- No `dotnet test` and no `dotnet build` were run due repository rule.