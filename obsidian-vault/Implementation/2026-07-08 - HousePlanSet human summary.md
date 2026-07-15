# 2026-07-08 - HousePlanSet human summary

## Type
Implementation

## What changed
- `MultiSheetExportAuditDto` now carries `HumanSummary` lines, so `manifest.json` can explain the export in human terms.
- `CreateMultiSheetExportAuditHandler` derives the summary from existing evidence only: canonical FloorPlan impact rows, Electrical export operation audit rows, and projection summary counts.
- `SitePlanAdjustmentViewModel` prepends those human summary lines in the UI audit panel before per-sheet technical rows.

## Why
The observability goal required the app to answer, without guessing: how many FloorPlan operations were applied, how many Electrical operations were applied, and whether warnings/failures exist.

## Verification
- `scripts/test-plan-set-human-summary-contract.ps1` was created RED first, then passed.
- Existing allowed checks passed: input audit contract, FloorPlan impact audit contract, audit artifact wiring, verifier self-check, and `git diff --check`.
- Latest runtime verifier still fails on an old stale manifest missing `input-audit.json`; fresh Desktop re-export is still required.

## Scope boundary
No new dashboard, no new Electrical solver, no Roof/Facade work, no build.
