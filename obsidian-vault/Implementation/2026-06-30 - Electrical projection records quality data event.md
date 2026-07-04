# Electrical projection records quality data event

## What changed
- `ProjectElectricalSheetAdjustmentHandler` now emits a best-effort `PlanSetAuditEvent` named `SheetAdjustmentProjectionQualityMeasured` after creating an electrical sheet projection.
- The payload captures plan-set version id, dependent sheet id, registration id, canonical adjustment id, projection method, confidence, status, warning, and canonical compression step count.
- The event uses the projection as aggregate: `AggregateType = SheetAdjustmentProjection`, `AggregateId = projection.Id`.

## Why
The HousePlanSet goal requires DataCollection to measure registration/projection quality, not just export outcomes. Electrical projection is the first dependent-sheet projection path and now produces quality telemetry at the moment the projection is created.

## Boundary
- This slice covers Electrical projection only.
- Roof and Facade/Elevation projection quality events are still pending.
- Telemetry is best-effort and must not block projection persistence.
- No new projection engine or analytics service was introduced.

## Verification
- Test-first RED: `ProjectElectricalSheetAdjustmentHandlerTests` now expects a `SheetAdjustmentProjectionQualityMeasured` event with confidence/status/method payload.
- GREEN: the handler emits the event through existing `IPlanSetAuditEventRepository` and keeps telemetry best-effort.
- DI already has `IPlanSetAuditEventRepository` registered, so Desktop runtime can inject it.
- `git diff --check` passed for touched files.
- No `dotnet test` and no `dotnet build` were run due repository rule.