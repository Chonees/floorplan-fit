# 2026-07-01 - Sheet import records classification quality events

- `ImportPlanSheetHandler` now records `SheetClassificationQualityMeasured` events for imported dependent sheets when an audit repository is available.
- Classification evidence is stored in existing `audit_events`: plan-set version, resolved sheet type, source (`Classifier` or `UserSelected`), confidence, status, and reason.
- `GetPlanSetQualityReportHandler` now maps classification events into quality `Signals` without changing registration/projection counts.
- `SqlitePlanSetAuditEventRepository` now includes classification events in quality-event reads.
- Boundary: no new table/repository/wizard; `audit_events` is enough until classification correction UX needs richer history.
- Verification: scoped `git diff --check` returned exit code 0 with CRLF warnings only; `rg`/`Select-String` verified event wiring and docs. No agent-run build/test due repo rule.
