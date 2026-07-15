# Windows backup destination pooling lock

## Status

Fixed in the current worktree by a minimal connection-policy opt-out. Executable proof remains pending because this task forbids all `dotnet` commands.

## Links

- `replaces`: the locked workspace-backup staging failure group in [[2026-07-13 - Executable P0 proof is red]].
- `replaced_by`: pending a permitted executable rerun.

## Root cause

`CreatePreMigrationBackupIfNeeded` opened its transient backup destination through the same pooled connection policy as long-lived production database use. On Windows, disposing that logical destination connection could return the physical SQLite connection to the pool, retaining the staging `app.db` handle when cleanup attempted to delete the staging directory.

## Fix

- `SqliteConnectionPolicy.Open` now accepts `bool pooling = true` and forwards it to `SqliteConnectionStringBuilder.Pooling`.
- Existing callers retain pooling through the default.
- Only the transient `BackupDatabase` destination passes `pooling: false`.
- No `ClearAllPools` call or global pooling change was introduced.

## Evidence

- Static inspection confirms the source connection still uses the default and only the backup destination opts out.
- Static inspection confirms `ClearAllPools` is absent from both touched production files.
- `git diff --check` exits `0` for the owned production files.
- No build, test, restore, watch or app command was run.

## Files

- `src/FloorplanFit.Infrastructure/Persistence/SqliteConnectionPolicy.cs`
- `src/FloorplanFit.Infrastructure/Runtime/AppWorkspaceDurability.cs`
