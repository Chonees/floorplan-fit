---
type: Bugs
date: 2026-07-01
replaces: []
replaced_by: null
---

# HousePlanSet package export skipped after adjustment scope disposed

## What
After the user exported a Seminole adjusted DXF, the main DXF was written but no HousePlanSet `manifest.json`, projected ElectricalPlan DXF, canonical adjustment, projection, or plan-set export rows were created.

## Evidence
- User saw only the selected output DXF in `D:\PointAIData\PLANS\original electrical plans`.
- `manifest.json` search under `src/FloorplanFit.Desktop/bin/Debug` returned no rows.
- SQLite inspection showed:
  - `plan_sheets`: 1
  - `sheet_registrations`: 1 confirmed electrical registration
  - `canonical_floor_plan_adjustments`: 0
  - `sheet_adjustment_projections`: 0
  - `plan_set_exports`: 0

## Root cause
`LibraryViewModel.OpenVersionSitePlanAdjustmentAsync(...)` created a scoped service provider with `using var scope`, passed scoped handlers into the long-lived `SitePlanAdjustmentViewModel`, then disposed the scope before the user clicked `Exportar DXF`. The main exporter still wrote the selected DXF, but HousePlanSet recording/export handlers could not be used reliably afterward.

## Fix
`LibraryViewModel` now keeps the adjustment scope alive while the site-plan adjustment screen is active and disposes it when leaving that screen or opening another adjustment/review flow.

## Verification
- Added static regression coverage that checks the adjustment scope is retained and disposed explicitly.
- Scoped `git diff --check` passed with CRLF warnings only.
- Runtime verification still requires restarting the app and exporting again.

## Files
- `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
