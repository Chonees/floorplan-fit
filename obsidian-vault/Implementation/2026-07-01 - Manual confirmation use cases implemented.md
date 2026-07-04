# Manual confirmation use cases implemented

## What changed
Added the minimal PlanSet confirmation slice so manual review states are actionable instead of only reported.

## Implemented
- `ConfirmSheetRegistrationHandler` confirms a pending sheet registration, persists `Confirmed`, sets `ConfirmedAtUtc`, and records a best-effort `SheetRegistrationQualityMeasured` event.
- `ConfirmSheetAdjustmentProjectionHandler` marks a manual projection as `ReadyForExport` only when its base registration is already confirmed, then records a best-effort `SheetAdjustmentProjectionQualityMeasured` event.
- `ISheetRegistrationRepository` and `ISheetAdjustmentProjectionRepository` now expose `UpdateAsync`; SQLite implementations persist the status changes.
- Desktop DI registers both confirmation handlers.
- `GetPlanSetQualityReportHandler` now uses the latest quality signal per aggregate so a confirmed registration/projection does not remain counted as manual forever.

## Boundaries
- No review screen or button was added yet.
- No reject workflow was added; confirmation was the smallest useful action to close the manual/export loop.
- No per-sheet fit engine was introduced.

## Verification
- Tests were written first for confirmation handlers, SQLite update persistence, and latest quality-signal reporting.
- RED evidence before production code: new tests referenced missing handlers/requests/update methods.
- Static verification: scoped `git diff --check` passed for touched tracked files; new files were checked for trailing whitespace.
- No `dotnet test` or `dotnet build` was run due repository rule.