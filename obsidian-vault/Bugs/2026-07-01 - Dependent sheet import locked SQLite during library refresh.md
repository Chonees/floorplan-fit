# 2026-07-01 - Dependent sheet import locked SQLite during library refresh

## Symptom
After selecting a floor-plan version and importing an ElectricalPlan sheet, the Desktop app crashed with:

`SQLite Error 5: 'database is locked'`

The stack trace showed `SqliteSession.OpenAsync(... BeginTransaction)` while `LibraryViewModel.ImportDependentSheetAsync(...)` was refreshing library items.

## Root cause
The prior transaction fix renewed the SQLite transaction immediately after every commit. During dependent sheet import, the import scope committed successfully but stayed alive while `RefreshItemsAsync(...)` opened a second scope/session. The first scope still held a newly-opened transaction, so SQLite rejected the second session transaction with `database is locked`.

## Fix
`SqliteSession` now creates transactions lazily:
- `OpenAsync(...)` opens only the connection.
- `Transaction` starts `BeginTransaction()` only when a repository command actually asks for it.
- `CommitAsync(...)` commits/disposes the active transaction and leaves it `null`.
- `Dispose` rolls back only if a transaction is active.

This preserves same-session repository use after commit without holding an idle transaction that locks later scopes.

## Regression coverage
Added `Committed_session_does_not_lock_database_before_it_is_disposed` to `PlanSetVersionPersistenceTests`. It commits in one session, keeps that session alive, and opens a second session before disposing the first.

## Verification
- Static verification confirmed `SqliteSession` no longer starts transactions in `OpenAsync` or immediately after commit.
- Static verification confirmed the regression test opens a second session before disposing the first.
- Scoped `git diff --check` passed.
- No agent-run build/test due repository rule: `Never build after changes`.

## Files
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSession.cs`
- `tests/FloorplanFit.Infrastructure.Tests/PlanSets/PlanSetVersionPersistenceTests.cs`
