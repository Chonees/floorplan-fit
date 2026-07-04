# 2026-07-01 - Plan sheet type correction use case introduced

- Added `CorrectPlanSheetTypeHandler` for correcting an imported dependent sheet type before registration.
- The handler rejects `FloorPlan` corrections because floor plans stay in the canonical import flow.
- The handler rejects corrections once the sheet already has a registration, preventing stale registration/projection data after a type change.
- `SqlitePlanSheetRepository.UpdateAsync(...)` updates `plan_sheets.sheet_type` only; no reimport is needed.
- Corrections record `SheetClassificationQualityMeasured` with `source=UserCorrection`, previous type, new type, confidence, and reason.
- Boundary: no UI wizard yet, no new table, no registration/projection recalculation, no dependent fit engine.
- Verification: scoped `git diff --check` returned exit code 0 with CRLF warnings only; `rg` verified handler, DTOs, repository update, DI wiring, docs, and tests. No agent-run build/test due repo rule.
