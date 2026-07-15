---
type: Bugs
date: 2026-07-09
replaces: []
replaced_by: null
---

# HousePlanSet package lost canonical audit context

## Problem
A fresh SEMINOLE export showed the UI reporting a ready HousePlanSet package, but the verifier failed:

`input-audit.json has no captured dimensionsInches`

The exported audit files showed:
- `input-audit.json`: `dimensionsInches: null`
- `canonical-recipe-audit.json`: `recipe: null`, `operationCount: 0`
- `floorplan-impact-audit.json`: `operations: []`
- `electrical-projection-audit.json`: 8 operations, 6 applied and 2 no-geometry affected

## Root cause
`SitePlanAdjustmentViewModel` built an `AdjustedSitePlanPlacementDto` containing `InputAudit` and `FloorPlanImpactAudit`, and the recorded canonical response had `AdjustmentRecipe`, but `ExportMultiSheetPlanSetPackageRequest` did not carry `CanonicalPlacement` or `CanonicalRecipe` into package audit creation. The audit handler attempted repository fallback, but the real export proved it returned null for this run.

## Fix
Added optional `CanonicalPlacement` and `CanonicalRecipe` pass-through:
- `ExportMultiSheetPlanSetPackageRequest`
- `CreateMultiSheetExportAuditRequest`
- `ExportMultiSheetPlanSetPackageHandler`
- `CreateMultiSheetExportAuditHandler`
- `SitePlanAdjustmentViewModel`

Also fixed the human summary contradiction: if Electrical operations have warnings/failures, the final warning line now reports them instead of saying none.

## Verification
Allowed checks run, no dotnet build:
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1` passed
- `scripts/test-plan-set-input-audit-contract.ps1` passed
- `scripts/test-plan-set-floorplan-impact-audit-contract.ps1` passed
- `scripts/test-plan-set-human-summary-contract.ps1` passed
- `scripts/test-plan-set-observability-audit-files.ps1` passed
- `git diff --check` on touched files passed with line-ending warnings only

The latest runtime verifier still fails against stale manifest `ba64fb54686c44ee8d40e86eb034b7a4`; a fresh app export is required after this fix.
