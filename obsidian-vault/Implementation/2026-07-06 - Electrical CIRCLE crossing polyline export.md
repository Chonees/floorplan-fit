# 2026-07-06 - Electrical CIRCLE crossing polyline export

## Type
Implementation / bugfix

## Context
Fresh compression export `2cf2ae537a974e41bdd1ae11212b7594/manifest.json` moved past the prior ARC blocker and exposed the next safe-geometry blocker: `CIRCLE crosses a canonical recipe pinch line`.

The user-facing `testYALOHIZE-plan-set` folder was empty because the canonical FloorPlan exported to `C:\Users\lucas\Downloads\testYALOHIZE.dxf`, but the dependent ElectricalPlan was downgraded to `RequiresManualConfirmation`, so no Electrical DXF was written into the plan-set folder.

## Change
- `ProjectedPlanSheetDxfExporter` now converts only recipe-crossing `CIRCLE` entities into closed sampled `LWPOLYLINE` geometry before projection.
- The closed polyline vertices then use the existing Electrical source -> FloorPlan recipe -> export path.
- Non-crossing CIRCLE entities remain CIRCLE.
- ELLIPSE crossing remains manual-review guarded.

## Verification
- `git diff --check` passed.
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1` passed.
- No build/test/watch was run, per repo rule.

## Still not final
A fresh desktop SEMINOLE compression export is required. The final proof remains:
`powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\verify-latest-plan-set-recipe-manifest.ps1" -RequireAutomatic`
