# DataCollection quality report implemented

## What changed
- Added `GetPlanSetQualityReportHandler` to summarize HousePlanSet quality events for a plan-set version.
- Added `IPlanSetAuditEventReader` as the read-side port for audit/data events.
- Added DTOs: `PlanSetQualityReportDto` and `PlanSetQualitySignalDto`.
- `SqlitePlanSetAuditEventRepository` now implements the reader and lists registration/projection quality events for a plan-set version.
- Desktop DI wires `IPlanSetAuditEventReader` and `GetPlanSetQualityReportHandler`.

## Why
DataCollection was capturing registration/projection quality events, but without a read model it was just stored telemetry. The HousePlanSet tool needs a minimal way to ask: how many registration/projection quality signals exist, what confidence floor do they show, and what needs manual review?

## Boundary
- No dashboard, charting, or analytics service was added.
- The SQLite reader uses the existing `audit_events` table and filters quality events by plan-set id in payload JSON.
- This is a minimal reporting boundary; richer analytics can wait for real usage.

## Verification
- Test-first RED: `GetPlanSetQualityReportHandlerTests` required a handler, reader port, report DTOs, counts, lowest confidence, manual counts, and signals.
- GREEN: implemented the handler, DTOs, reader port, SQLite reader, and DI wiring.
- `git diff --check` passed for touched files.
- No `dotnet test` and no `dotnet build` were run due repository rule.