# Desktop confirms dependent sheet projection

Date: 2026-07-01
Type: Implementation
Status: Current

## What changed
- Dependent HousePlanSet sheet rows now carry `SheetProjectionId` in addition to registration state.
- `GetPlanSetLibraryHandler` overlays the latest `SheetAdjustmentProjection` status/id onto each dependent sheet row.
- Library UI exposes `Confirm Projection` only when a dependent sheet projection is `RequiresManualConfirmation`.
- `LibraryViewModel.ConfirmDependentSheetProjectionAsync(...)` calls `ConfirmSheetAdjustmentProjectionHandler`, refreshes the selected HousePlanSet sheet list, and preserves the selected floor-plan version.

## Why
Registration and projection are different gates. A sheet can be correctly associated to the canonical floor plan but still require manual projection approval before it is safe for package export.

## Boundary
- No new per-sheet fit engine.
- No geometry picker or roof/facade-specific review UI in this slice.
- No agent-run build/test because this repository explicitly forbids build after changes.

## Verification
- Existing RED assertions target read-model projection status/id, layout `Confirm Projection`, and ViewModel confirmation/refresh behavior.
- Ran scoped `git diff --check`; exit code 0, only CRLF warnings.

## Related
- [[2026-07-01 - Desktop confirms dependent sheet registration]]
- [[2026-07-01 - Desktop registers electrical sheet from library]]
