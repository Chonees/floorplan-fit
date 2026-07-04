# Canonical recipe route integration test added

## What
Added an application-level route test that connects the first demonstrable AdjustmentRecipe loop in one story:

1. `RecordCanonicalFloorPlanAdjustmentHandler` records a canonical FloorPlan adjustment.
2. The saved adjustment contains `AdjustmentRecipeJson` with `HorizontalCompression`.
3. `ProjectRegisteredPlanSetSheetsHandler` receives `canonicalAdjustment.AdjustmentRecipe`.
4. Electrical projection consumes that recipe, computes the global transform, and marks local compression as review-required.
5. `CreateMultiSheetExportAuditHandler` carries `RecipeHandlingSummary` into the audit and manifest writer payload.

## File
- `tests/FloorplanFit.Application.Tests/PlanSets/Adjustment/CanonicalAdjustmentRecipeRouteTests.cs`

## Why
The goal definition is not just separate unit behavior. It requires a route:
FloorPlan adjustment -> AdjustmentRecipe saved -> dependent sheet projection consumes the recipe -> export report indicates applied vs review.

This test is the smallest non-runtime proof of that route without introducing a new engine or DXF deformation logic.

## Verification
- `rg` confirms route assertions and recipe/report fields across source and tests.
- `git diff --check` exited 0 with CRLF warnings only for tracked files.
- Explicit whitespace check on the new untracked test file returned `no trailing whitespace in route test`.
- Build/test intentionally not run because repo instructions prohibit building after changes.

## Still not complete
Runtime SEMINOLE verification is still pending:
- SEMINOLE FloorPlan
- SEMINOLE ElectricalPlan
- patio/porch/living compression case
- real package folder
- real manifest/report inspection
