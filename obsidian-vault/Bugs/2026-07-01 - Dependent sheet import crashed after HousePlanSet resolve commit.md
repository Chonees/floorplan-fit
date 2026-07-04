# 2026-07-01 - Dependent sheet import crashed after HousePlanSet resolve commit

## Symptom
When the Desktop app imported an ElectricalPlan dependent sheet after selecting a floor-plan version, it crashed with:

`System.InvalidOperationException: The transaction object is not associated with the same connection object as this command.`

The stack trace pointed to `SqlitePlanSetVersionRepository.GetByCanonicalFloorPlanVersionAsync(...)` during `LibraryViewModel.ImportDependentSheetAsync(...)`.

## Root cause
`ImportDependentSheetAsync` resolves a HousePlanSet and then a PlanSetVersion inside the same DI scope. `ResolveHousePlanSetHandler` can call `IUnitOfWork.SaveChangesAsync(...)`, which commits the current `SqliteSession.Transaction`. The same scope then tries to use another repository with the already-committed transaction, so Microsoft.Data.Sqlite rejects the command/transaction association.

## Fix
`SqliteSession.CommitAsync(...)` now commits and disposes the current transaction, then immediately starts a fresh transaction on the same connection. This keeps scoped repositories usable after a unit-of-work commit.

## Regression coverage
Added `Session_allows_repository_commands_after_unit_of_work_commit` to `PlanSetVersionPersistenceTests`. It reproduces the flow: write HousePlanSet, commit, then read/add PlanSetVersion using the same session.

## Verification
- Static verification confirmed `SqliteSession` renews `Transaction` after commit.
- Static verification confirmed the regression test covers commit-then-repository-use.
- Scoped `git diff --check` passed.
- No local `dotnet test`/build run by agent due repository rule: `Never build after changes`.

## Files
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSession.cs`
- `tests/FloorplanFit.Infrastructure.Tests/PlanSets/PlanSetVersionPersistenceTests.cs`
