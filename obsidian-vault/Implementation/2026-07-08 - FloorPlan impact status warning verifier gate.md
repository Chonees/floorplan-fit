# 2026-07-08 - FloorPlan impact status warning verifier gate

## Type
Implementation

## What changed
- `verify-latest-plan-set-recipe-manifest.ps1` now requires each `floorplan-impact-audit.json` operation row to include `Status`.
- Accepted FloorPlan impact statuses are `Applied` and `NoGeometryAffected`.
- If a FloorPlan operation affects zero vertices, the verifier now requires a non-empty `Warning`.
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1` now includes a RED/GREEN fixture where a `NoGeometryAffected` FloorPlan operation without warning is rejected.

## Why
The observability goal explicitly requires warnings when a FloorPlan operation affects nothing. Without this gate, a no-op FloorPlan compression could pass silently.

## Verification
- RED observed: self-check failed with `Expected FloorPlan no-impact operation without warning to fail.`
- GREEN observed after adding the verifier gate.
- Allowed checks passed: verifier self-check, human summary contract, input audit contract, FloorPlan impact audit contract, audit artifact wiring, and `git diff --check`.
- Latest runtime verifier still fails on stale manifest missing `input-audit.json`; fresh Desktop re-export remains required.

## Scope boundary
No build, no exporter rewrite, no new Electrical solver, no Roof/Facade work.
