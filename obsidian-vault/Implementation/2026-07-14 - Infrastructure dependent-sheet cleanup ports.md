# 2026-07-14 - Infrastructure dependent-sheet cleanup ports

## Scope

Loop 2 / Plan Set Library persistence implementation following [[2026-07-14 - Application unlink RED contract]].

## What changed

- `SqliteSheetAdjustmentProjectionRepository.RemoveByDependentSheetIdAsync` deletes only rows in `sheet_adjustment_projections` whose `dependent_sheet_id` equals the supplied sheet ID.
- `SqliteSheetRegistrationRepository.RemoveByDependentSheetIdAsync` deletes only rows in `sheet_registrations` whose `dependent_sheet_id` equals the supplied sheet ID.
- Each method checks the supplied cancellation token, uses the repository's current SQLite session/transaction, executes one parameterized `DELETE`, and returns `Task.CompletedTask`.

## Boundary

`SqlitePlanSheetRepository.RemoveAsync` is unchanged: `plan_sheets` has `id`, not `dependent_sheet_id`. Application owns the transaction order: projections, registrations, then the sheet, followed by exactly one UnitOfWork commit. Imported documents and historical export/audit snapshots remain outside this change.

## Evidence

- Scoped `git diff --check` passed.
- Static source-shape inspection confirmed both methods, their parameterized delete scope, and cancellation checks.
- No `dotnet`, build, test, restore, watch, Desktop, or runtime command was run. Runtime GREEN remains unproven.

## Related

- [[2026-07-14 - Application unlink RED contract]]
- [[2026-07-14 - Confirmed ElectricalPlan unlink leaves active workflow references]]
