# 2026-07-08 - HousePlanSet input audit dimensions

## Type
Implementation

## Context
The HousePlanSet observability goal requires input-audit.json to answer what the user asked to fit: original width/height, requested width/height, and required deltas in inches.

## What changed
- Added `AdjustmentInputAuditDto` to the adjusted placement contract.
- `SitePlanAdjustmentViewModel.BuildAdjustedSitePlanPlacement()` now attaches an `InputAudit` derived from the current fit preview.
- `PlanSetExportManifestWriter` writes `dimensionsInches` into `audit/input-audit.json` instead of the old `UnavailableInCurrentExportContract` placeholder.
- `verify-latest-plan-set-recipe-manifest.ps1` now rejects package exports that do not contain captured input dimensions.
- Added `scripts/test-plan-set-input-audit-contract.ps1` as the smallest repo-allowed check for this contract.

## Why
Without this, the package could explain the recipe/electrical projection but still could not answer the first user question: “how much did I ask the system to shrink?” That made the audit incomplete.

## Verification run
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-plan-set-input-audit-contract.ps1`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-plan-set-observability-audit-files.ps1`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-verify-latest-plan-set-recipe-manifest.ps1`
- `git diff --check`

## Important limitation
No `dotnet build` or `dotnet test` was run because AGENTS.md forbids builds after changes. Runtime proof still requires restarting the desktop app and exporting a fresh HousePlanSet package.

## Next
Generate a fresh SEMINOLE compression export and run the verifier. Old manifests fail by design because they lack the new `audit/input-audit.json` dimensions.

## 2026-07-08 - Electrical projection operation audit gate
- Strengthened `verify-latest-plan-set-recipe-manifest.ps1`: compression exports now fail if `electrical-projection-audit.json` has fewer operation entries than `canonical-recipe-audit.json.operationCount`.
- Added verifier self-test coverage for missing Electrical projection operation audit.
- This closes the silent-failure class where the FloorPlan had compression operations but Electrical had no per-operation evidence.
