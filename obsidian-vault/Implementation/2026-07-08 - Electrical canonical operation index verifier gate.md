# 2026-07-08 - Electrical canonical operation index verifier gate

## Type
Implementation

## What changed
- `verify-latest-plan-set-recipe-manifest.ps1` now requires Electrical projection audit rows to cover every canonical operation index exactly once.
- For a canonical recipe with `operationCount = N`, Electrical must report exactly one row for each `OperationIndex` from `0` to `N - 1`.
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1` now includes a RED/GREEN fixture where Electrical reports two operation rows but duplicates `OperationIndex = 0`, leaving canonical operation `1` silent.

## Why
Counting Electrical operations was not enough. Electrical could report the same operation twice and silently omit another canonical operation, including a vertical compression. The goal requires each FloorPlan operation to be traceable in Electrical.

## Verification
- RED observed: self-check failed with `Expected manifest missing an Electrical canonical operation index to fail.`
- GREEN observed after adding the verifier gate.
- Allowed checks passed: verifier self-check, human summary contract, input audit contract, FloorPlan impact audit contract, audit artifact wiring, and `git diff --check`.
- Latest runtime verifier still fails on stale manifest missing `input-audit.json`; fresh Desktop re-export remains required.

## Scope boundary
No build, no exporter rewrite, no new Electrical solver, no Roof/Facade work.
