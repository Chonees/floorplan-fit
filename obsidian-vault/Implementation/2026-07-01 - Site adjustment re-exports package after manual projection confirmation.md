# Site adjustment re-exports package after manual projection confirmation

Date: 2026-07-01
Type: Implementation
Status: Current

## What changed
- `SitePlanAdjustmentWindow` now exposes `Confirmar manuales + re-exportar` after an export audit contains dependent projections in `RequiresManualConfirmation`.
- `SitePlanAdjustmentViewModel.ConfirmManualPlanSetProjectionsAndReExportAsync(...)` confirms the manual projection ids from the current audit and re-runs the package export with the same `LastCanonicalAdjustmentId`.
- `LibraryViewModel` now passes the registered `ConfirmSheetAdjustmentProjectionHandler` into the site-plan adjustment ViewModel so the command can be enabled from the adjustment window.

## Why
The Library could confirm projection rows one by one, but the adjustment/export screen had no direct way to re-export the same package after manual approval. Without this, the user could approve a projection and still have no obvious next step besides rerunning the full adjustment, which may create a new canonical adjustment and recreate review work.

## Boundary
- This is not a new fit engine.
- It does not create a geometry picker or visual projection editor.
- It reuses the already exported canonical DXF path and existing canonical adjustment id from the current audit.
- No agent-run build/test because this repository explicitly forbids build after changes.

## Verification
- Added/kept ViewModel coverage for reusing the existing canonical adjustment when confirming manual projections and re-exporting.
- Added/kept layout assertion for the new adjustment-window action.
- Ran scoped `git diff --check`; exit code 0, only CRLF warnings.
- Verified DI registration exists for `ConfirmSheetAdjustmentProjectionHandler` and the command/wiring symbols exist with `rg`.

## Related
- [[2026-07-01 - Desktop confirms dependent sheet projection]]
- [[2026-07-01 - HousePlanSet design spec updated to current export loop]]
