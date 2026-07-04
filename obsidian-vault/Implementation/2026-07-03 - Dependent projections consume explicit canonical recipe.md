# Dependent projections consume explicit canonical recipe

## What
Dependent sheet projections now accept and consume an explicit `AdjustmentRecipeSummaryDto` instead of always deriving recipe handling from `AdjustedSitePlanPlacementDto` inside each projector.

The flow is now:
1. FloorPlan export builds `AdjustedSitePlanPlacementDto`.
2. `RecordCanonicalFloorPlanAdjustmentHandler` derives `AdjustmentRecipeSummaryDto` once.
3. The canonical adjustment stores `AdjustmentRecipeJson` and returns the recipe in `RecordCanonicalFloorPlanAdjustmentResponse`.
4. Desktop passes `canonicalAdjustment.AdjustmentRecipe` into `ProjectRegisteredPlanSetSheetsRequest`.
5. Electrical/Roof/Facade projectors use `CanonicalRecipe` for:
   - global transform scale/offset,
   - compression operation count,
   - recipe handling summary / review message.

## Why
The active goal requires: FloorPlan adjustment -> AdjustmentRecipe saved -> dependent sheet projection consumes the recipe -> export/report indicates applied vs review.

Before this slice, dependent projections could derive a recipe-like summary from placement, but they did not explicitly consume the saved canonical recipe object. This made the route conceptually weaker.

## Files
- `src/FloorplanFit.Contracts/PlanSets/RecordCanonicalFloorPlanAdjustmentResponse.cs`
- `src/FloorplanFit.Contracts/PlanSets/ProjectRegisteredPlanSetSheetsRequest.cs`
- `src/FloorplanFit.Contracts/PlanSets/ProjectElectricalSheetAdjustmentRequest.cs`
- `src/FloorplanFit.Contracts/PlanSets/ProjectRoofSheetAdjustmentRequest.cs`
- `src/FloorplanFit.Contracts/PlanSets/ProjectFacadeElevationSheetAdjustmentRequest.cs`
- `src/FloorplanFit.Application/PlanSets/Adjustment/RecordCanonicalFloorPlanAdjustmentHandler.cs`
- `src/FloorplanFit.Application/PlanSets/Projection/ProjectRegisteredPlanSetSheetsHandler.cs`
- `src/FloorplanFit.Application/PlanSets/Projection/ProjectElectricalSheetAdjustmentHandler.cs`
- `src/FloorplanFit.Application/PlanSets/Projection/ProjectRoofSheetAdjustmentHandler.cs`
- `src/FloorplanFit.Application/PlanSets/Projection/ProjectFacadeElevationSheetAdjustmentHandler.cs`
- `src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs`
- focused tests in Application.Tests.

## Verification
- `rg` confirmed `CanonicalRecipe` flow across contracts, handlers, Desktop, and tests.
- `git diff --check` exited 0 with CRLF warnings only.
- Build/test intentionally not run because repo instructions prohibit building after changes.

## Out of scope
This still does not replay local compression into Electrical DXF geometry. It reports/reviews the local operation safely; actual entity-level deformation remains a later slice.
