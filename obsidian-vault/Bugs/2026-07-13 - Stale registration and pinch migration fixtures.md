# Stale registration and pinch migration fixtures

## Status

Fixed in the current worktree by aligning only the stale test fixtures with the current identity and migration gates. Executable proof remains pending because this task forbids all `dotnet` commands.

## Links

- `updates`: the sheet-registration and legacy pinch-marker fixture failure groups in [[2026-07-13 - Executable P0 proof is red]].
- `replaced_by`: pending a permitted executable rerun.

## Root cause

- Two sheet-registration persistence tests inserted registrations without the `FloorPlanVersion`, `PlanSetVersion`, and owned dependent `PlanSheet` rows now required by the canonical identity trigger.
- The confirmation update also fabricated new ownership IDs instead of preserving the registration identity.
- Three pinch-marker migration tests initialized the current schema, replaced the current table with a legacy shape, but left `PRAGMA user_version` at the current version, so the initializer correctly skipped the legacy migration.

## Fix

- A typed test helper now seeds deduplicated canonical floor-plan versions, plan-set versions, and dependent sheets before registration inserts.
- The confirmed registration preserves the original PlanSet, dependent-sheet, and canonical-floor-plan identity.
- Exactly the three legacy pinch-marker SQL setups set `PRAGMA user_version = 0` before rerunning the initializer.
- No production trigger, migration, backup, identity, or DXF safety gate changed.

## Evidence

- Targeted static inspection confirms the two registration tests call the ownership seed before inserts.
- Targeted static inspection confirms exactly three legacy fixture resets.
- `git diff --check` exits `0` for the two owned test files.
- No build, test, restore, watch, app, or other `dotnet` command was run.

## Files

- `tests/FloorplanFit.Infrastructure.Tests/PlanSets/SheetConfirmationPersistenceTests.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Curation/FloorPlanCurationPersistenceIntegrationTests.cs`
