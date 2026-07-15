# 2026-07-08 - Electrical projection operation audit verifier gate

## Type
Implementation

## What changed
- `verify-latest-plan-set-recipe-manifest.ps1` now validates every ElectricalPlan projection operation row.
- Required operation evidence now includes:
  - `OperationId`
  - `OperationIndex`
  - `Kind`
  - `AxisTag`
  - `Edge`
  - `Coordinate`
  - `ExpectedDeltaSourceUnits`
  - `AffectedEntities`
  - `AffectedVertices`
  - `MeasuredMinDeltaSourceUnits`
  - `MeasuredMaxDeltaSourceUnits`
  - `Status`
- Accepted statuses are only `Applied`, `NoGeometryAffected`, `RequiresManualReview`, or `Failed`.
- Any non-`Applied` operation must include a reason.
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1` now has a RED/GREEN fixture proving that `Status`-only Electrical operations are rejected.

## Why
The HousePlanSet observability goal requires Electrical projection audit to explain what happened per canonical FloorPlan operation. A row with only `Status` is not enough evidence.

## Verification
- RED observed: self-check failed with `Expected manifest with incomplete electrical projection operation fields to fail.`
- GREEN observed after adding the verifier gate.
- Allowed checks passed: verifier self-check, human summary contract, input audit contract, FloorPlan impact audit contract, audit artifact wiring, and `git diff --check`.
- Latest runtime verifier still fails on stale manifest missing `input-audit.json`; fresh Desktop re-export remains required.

## Scope boundary
No build, no exporter rewrite, no new Electrical solver, no Roof/Facade work.
