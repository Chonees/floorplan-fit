# 2026-07-06 - Electrical ARC crossing polyline export

## Type
Implementation / bugfix

## Context
Latest SEMINOLE compression manifest `0afa3c787fb34a98b63c10ba175913e3/manifest.json` had `CompressionRecipeSheets = 1` but ElectricalPlan stayed `RequiresManualConfirmation` because an `ARC` crossed a canonical recipe pinch line.

## Change
- `ProjectedPlanSheetDxfExporter` now replaces only recipe-crossing `ARC` entities with sampled `LWPOLYLINE` geometry before projection.
- The generated polyline points then flow through the existing Electrical source -> FloorPlan recipe -> export projection path.
- Basic CAD styling is preserved from the original ARC: layer, color, linetype/lineweight-style pairs.
- Non-crossing ARC entities remain ARC.
- CIRCLE/ELLIPSE crossing behavior remains manual-review guarded.

## Verification
- `git diff --check` passed.
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1` passed.
- No build/test/watch was run, per repo rule.

## Still not final
A fresh desktop SEMINOLE compression export is still required before the active goal can be marked complete. The final proof remains:
`powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\verify-latest-plan-set-recipe-manifest.ps1" -RequireAutomatic`
