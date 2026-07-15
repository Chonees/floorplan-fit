# 2026-07-08 - FloorPlan impact audit rows

## Type
Implementation

## Context
The HousePlanSet observability goal requires `floorplan-impact-audit.json` to explain which canonical FloorPlan operations actually affected FloorPlan geometry before Electrical receives the same recipe.

## What changed
- Added `FloorPlanAdjustmentOperationImpactDto` to `AdjustedSitePlanPlacementDto`.
- `SitePlanAdjustmentViewModel.ApplyAutoFitPlan()` now records a floor-plan impact audit row for each canonical compression marker while the preview applies auto-fit.
- Each row records operation id/index, kind, axis, edge, coordinate, expected delta, affected entities, affected vertices, measured min/max delta, status, and warning.
- `PlanSetExportManifestWriter` now writes those persisted rows into `audit/floorplan-impact-audit.json` instead of the old placeholder.
- The verifier rejects compression manifests where `floorplan-impact-audit.json` has fewer operation rows than `canonical-recipe-audit.json.operationCount`.

## Verification run
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-plan-set-floorplan-impact-audit-contract.ps1`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-plan-set-input-audit-contract.ps1`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-plan-set-observability-audit-files.ps1`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-verify-latest-plan-set-recipe-manifest.ps1`
- `git diff --check`

## Current runtime state
The latest old desktop export still fails the verifier because it predates the audit folder: missing `input-audit.json`. A fresh desktop re-export is required before runtime proof can pass.

## Limitation
No `dotnet build` / `dotnet test` was run because AGENTS.md forbids builds after changes. The row counts are segment-endpoint based; upgrade to stable unique CAD vertex identity only if the audit needs CAD-object-perfect counts later.
