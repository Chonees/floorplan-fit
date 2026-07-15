# 2026-07-08 - HousePlanSet observability audit artifacts

## Type
Implementation

## What changed
HousePlanSet package export now has a minimal observability path for adjustment/export transparency.

## Current behavior
- `PlanSetExportManifestWriter` writes an `audit/` folder next to `manifest.json`.
- Required artifacts:
  - `input-audit.json`
  - `canonical-recipe-audit.json`
  - `floorplan-impact-audit.json`
  - `electrical-registration-audit.json`
  - `electrical-projection-audit.json`
  - `dxf-safety-audit.json`
- `ExportProjectedPlanSheetHandler` captures optional DXF/export audit evidence from the projected sheet exporter.
- `ProjectedPlanSheetDxfExporter` now counts canonical recipe operation impact per Electrical DXF operation: affected entities, affected vertices, measured delta/status.
- `ProjectedPlanSheetDxfExporter` also returns DXF safety counts for output existence, entity classes, missing handles/owners, and unsupported crossing count.
- `verify-latest-plan-set-recipe-manifest.ps1` now requires the audit files and fails final automatic proof if Electrical projection audit contains `Failed` operations.

## Important limitation
The export contract still does not persist explicit user-facing original/requested dimensions like `39 -> 38.7` and `77.5 -> 77.4`; `input-audit.json` therefore marks explicit dimensions as `UnavailableInCurrentExportContract` and derives required deltas from the canonical recipe only.

## Verification run
- `scripts/test-plan-set-observability-audit-files.ps1` passed.
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1` passed.
- `git diff --check` passed.
- No build/test command was run because AGENTS.md forbids builds after changes.

## Next proof needed
Run the desktop app, create a fresh SEMINOLE compression export, then run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\verify-latest-plan-set-recipe-manifest.ps1" -RequireAutomatic
```

The latest old runtime manifest will fail until re-exported because older packages do not contain the new `audit/` folder.
