# PlanSetExportedSheet enforces projection status consistency

Date: 2026-07-01
Type: Implementation
Status: Current

## What changed
- `PlanSetExportedSheet` now requires `SheetProjectionId` when status is `ProjectedAutomatically` or `RequiresManualConfirmation`.
- `PlanSetExportedSheet` now rejects `MissingProjection` rows that carry a projection id or exported storage path.
- Added focused tests for both status/identity invariants.

## Why
The multi-sheet export audit is the trust report for a HousePlanSet package. A row that claims a sheet was projected, but has no projection id, cannot be traced. A row that claims projection is missing, but carries projection output, contradicts itself.

## Boundary
- No export orchestration changed.
- No new service, repository, or fit engine.
- No agent-run build/test because this repository explicitly forbids build after changes.

## Verification
- Ran scoped `git diff --check`; exit code 0, only CRLF warnings.
- Ran `rg` for the new guard messages and test symbols.

## Related
- [[2026-07-01 - PlanSetExport rejects null sheet collection]]
- [[2026-07-01 - Site adjustment re-exports package after manual projection confirmation]]
