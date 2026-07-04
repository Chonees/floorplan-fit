# HousePlanSet Phase 7 multi sheet export audit implemented

## What
Implemented the first Phase 7 MultiSheetExportAudit/DataCollection slice.

## Why
The system now needs to explain a full house package after one canonical floor-plan adjustment: which sheets are canonical, which dependent sheets were automatically projected, which require manual confirmation, and with what confidence/warnings.

## Current truth
- Commits:
  - `43dbf87` (`docs: plan multi sheet export audit`)
  - `a07bf40` (`feat: audit multi sheet exports`)
  - `3e8080b` (`docs: record multi sheet export audit bridge`)
- New handler: `CreateMultiSheetExportAuditHandler`.
- New persisted export audit tables:
  - `plan_set_exports`
  - `plan_set_exported_sheets`
- New first DataCollection sink:
  - `audit_events`
- The audit includes the canonical floor-plan export path and explicit dependent projection ids.
- Dependent projections with `ReadyForExport` become `ProjectedAutomatically` in the package audit.
- Dependent projections with `RequiresManualConfirmation` stay in the audit as manual-review sheets instead of being silently trusted.
- Audit rows carry projection method, confidence, warning, and rule summary.
- `PlanSetExportAuditCreated` audit events are best-effort: telemetry failure does not block export audit persistence.

## Boundaries
- No dependent-sheet DXF rewrite/export yet.
- No missing-projection discovery.
- No recalculation of fit, registration, or projection.
- No Desktop UI.
- No per-sheet fit engine.

## Verification
- Test-first RED evidence: `CreateMultiSheetExportAuditHandler.cs` and `PlanSetExport.cs` did not exist before implementation (`Test-Path` returned `False`).
- Scoped `git diff --check` and `git diff --cached --check` passed.
- No `dotnet test` and no `dotnet build` were run due repository rule.
