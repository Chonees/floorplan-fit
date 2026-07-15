# 2026-07-10 - UI live segment congruence summary

## Type
Implementation

## Status
Implemented; fresh runtime export pending

## Context
The segment audit artifact was being written, but the UI human summary only said to review `outline-segment-congruence-audit.json`. The goal requires observability in plain text, not only raw JSON.

## Change
`SitePlanAdjustmentViewModel.BuildPlanSetExportAuditLines` now reads the package `audit/outline-segment-congruence-audit.json` and replaces the generic segment line with a concise human-readable result:
- `SegmentCongruent`: outline OK plus advisory internal differences.
- `InsufficientData`: explains missing evidence.
- Other statuses: says NO OK and how many required outline runs are missing.

## Verification
Allowed checks passed without dotnet build/test/watch:
- `scripts/test-plan-set-human-summary-contract.ps1`
- `scripts/test-outline-segment-congruence-contract.ps1`
- `scripts/test-outline-congruence-contract.ps1`
- `scripts/test-plan-set-observability-audit-files.ps1`
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1`
- `git diff --check` on touched files

## Next
Generate a fresh Desktop export after this UI/audit change and run the verifier with `-RequireAutomatic`.
