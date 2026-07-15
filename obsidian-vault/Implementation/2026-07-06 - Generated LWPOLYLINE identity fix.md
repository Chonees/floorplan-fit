# 2026-07-06 - Generated LWPOLYLINE identity fix for AutoCAD validity

## Type
Bugfix

## Context
The final verifier passed against `19595bf5a2f749118b9f7a3e2291d79e/manifest.json`, but AutoCAD opened the Electrical DXF as invalid/black. The DXF existed and was a non-empty AutoCAD Binary DXF, so the issue was not missing export output.

## Evidence
A binary DXF pair scan found 4 generated `LWPOLYLINE` entities missing DXF identity/owner pairs:
- missing group `5` handle
- missing group `330` owner

The original replaced `ARC`/`CIRCLE` entities had `5` and `330`, so the conversion lost required CAD identity metadata.

## Change
- `ProjectedPlanSheetDxfExporter` now preserves `5` and `330` when replacing crossing `ARC`/`CIRCLE` entities with `LWPOLYLINE`.
- Tests for crossing ARC and CIRCLE now assert the generated LWPOLYLINE keeps those pairs.
- Also preserves common layout/style pairs such as `67` and `410`.

## Verification
- `git diff --check` passed.
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1` passed.
- No build/test/watch was run, per repo rule.

## Still not final
Need a fresh SEMINOLE compression export and then manually open the new Electrical DXF in AutoCAD. The script passing alone is no longer sufficient after this CAD-validity bug.
