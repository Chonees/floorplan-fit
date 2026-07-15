# 2026-07-08 - DXF safety audit verifier gate

## Type
Implementation

## What changed
- `verify-latest-plan-set-recipe-manifest.ps1` now reads `audit/dxf-safety-audit.json` for `ProjectedAutomatically` ElectricalPlan exports.
- The verifier now fails if the ElectricalPlan DXF safety row is missing or reports:
  - missing/empty output file;
  - missing before/after entity counts;
  - missing INSERT/DIMENSION/ELLIPSE/wire classes;
  - missing handles;
  - missing owners;
  - unsupported crossing entities.
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1` now includes a RED/GREEN case where a valid-looking DXF is rejected because `dxf-safety-audit.json` reports `UnsupportedCrossingEntityCount = 1`.

## Why
The observability goal requires `dxf-safety-audit.json` to be proof, not decoration. Before this, the verifier checked the DXF file directly but did not fail on unsafe values reported by the audit artifact.

## Verification
- RED observed: self-check failed with `Expected manifest with unsafe dxf-safety-audit to fail.`
- GREEN observed after verifier gate.
- Allowed checks passed: verifier self-check, human summary contract, input audit contract, FloorPlan impact audit contract, audit artifact wiring, and `git diff --check`.
- Latest runtime verifier still fails on stale manifest missing `input-audit.json`; fresh Desktop re-export remains required.

## Scope boundary
No build, no exporter rewrite, no Electrical solver changes, no Roof/Facade changes.
