# PlanSetExportedSheet requires output only for exported sheets

Date: 2026-07-01
Type: Implementation
Status: Current

## What changed
- `PlanSetExportedSheet` now requires a storage path for `Exported` and `ProjectedAutomatically` rows.
- `RequiresManualConfirmation` rows now reject storage paths, because untrusted/manual projections should not pretend to be exported package output.
- Discovery audit tests now pass an exported dependent path when they expect `ProjectedAutomatically`.

## Why
The package audit must distinguish exported artifacts from review blockers. A projected-auto sheet without a file path is not a coherent package artifact; a manual-review sheet with a file path implies untrusted output was exported.

## Boundary
- No new status enum.
- No new export orchestrator or fit engine.
- No agent-run build/test because this repository explicitly forbids build after changes.

## Verification
- Ran scoped `git diff --check`; exit code 0, only CRLF warnings.
- Ran `rg` for the new guard messages/test symbols.

## Related
- [[2026-07-01 - PlanSetExportedSheet enforces projection status consistency]]
- [[2026-07-01 - PlanSetExport rejects null sheet collection]]
