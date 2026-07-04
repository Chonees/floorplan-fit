# PlanSetExport rejects null sheet collection

Date: 2026-07-01
Type: Implementation
Status: Current

## What changed
- `PlanSetExport` now rejects a null exported-sheet collection with `ArgumentNullException` before checking count.
- Added `PlanSetExportTests.Constructor_rejects_null_exported_sheets`.

## Why
A multi-sheet package audit must always have an explicit sheet collection. A null collection would crash with an accidental `NullReferenceException` instead of a clear domain-boundary error.

## Boundary
- No export behavior changed.
- No new service, repository, or abstraction.
- No agent-run build/test because this repository explicitly forbids build after changes.

## Verification
- Ran scoped `git diff --check`; exit code 0, only CRLF warnings.
- Ran `rg` for the new test and guard.

## Related
- [[2026-07-01 - PlanSheet rejects empty identity references]]
- [[2026-07-01 - Site adjustment re-exports package after manual projection confirmation]]
