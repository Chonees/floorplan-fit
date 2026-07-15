# 2026-07-08 - Canonical recipe operations verifier gate

## Type
Implementation

## What changed
- `verify-latest-plan-set-recipe-manifest.ps1` now requires `canonical-recipe-audit.json` to list actual recipe operations, not only `operationCount`.
- For compression proofs, `recipe.Operations.Count` must equal `operationCount`.
- Each canonical operation must include `Kind`, `AxisTag`, `Edge`, `Coordinate`, and `DeltaSourceUnits`.
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1` now has a RED/GREEN fixture proving that `operationCount` without listed recipe operations is rejected.

## Why
The observability goal requires evidence of what the FloorPlan solver generated. A naked count does not explain which operations were generated or how Electrical should mirror them.

## Verification
- RED observed: self-check failed with `Expected manifest without canonical recipe operations to fail.`
- GREEN observed after adding the verifier gate.
- Allowed checks passed: verifier self-check, human summary contract, input audit contract, FloorPlan impact audit contract, audit artifact wiring, and `git diff --check`.
- Latest runtime verifier still fails on stale manifest missing `input-audit.json`; fresh Desktop re-export remains required.

## Gotcha
PowerShell scalarizes single-element JSON arrays; the verifier casts canonical recipe operations to `[object[]]` so `.Count` is reliable.

## Scope boundary
No build, no exporter rewrite, no new Electrical solver, no Roof/Facade work.
