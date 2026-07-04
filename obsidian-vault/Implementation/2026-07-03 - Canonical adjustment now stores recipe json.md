# Canonical adjustment now stores recipe json

## What
`CanonicalFloorPlanAdjustment` now stores an explicit `AdjustmentRecipeJson` beside the existing `PlacementJson`.

The saved recipe is derived from `AdjustedSitePlanPlacementDto` and includes:
- `Version`
- global placement: `FloorToSiteScale`, `SiteOffsetX`, `SiteOffsetY`
- local operations: `HorizontalCompression` / `VerticalCompression` from compression markers

## Why
The active HousePlanSet goal requires a first demonstrable route:
FloorPlan adjustment -> AdjustmentRecipe saved -> dependent sheet projection consumes recipe -> export report indicates applied vs review.

Before this slice, the app persisted `placement_json`; that contained enough raw data, but the recipe was implicit. This change makes the recipe explicit without adding a new table or a new adjustment engine.

## Files
- `src/FloorplanFit.Domain/PlanSets/CanonicalFloorPlanAdjustment.cs`
- `src/FloorplanFit.Application/PlanSets/Adjustment/RecordCanonicalFloorPlanAdjustmentHandler.cs`
- `src/FloorplanFit.Contracts/FloorPlans/AdjustedSitePlanPlacementDto.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteCanonicalFloorPlanAdjustmentRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- `tests/FloorplanFit.Application.Tests/PlanSets/Adjustment/RecordCanonicalFloorPlanAdjustmentHandlerTests.cs`
- `tests/FloorplanFit.Application.Tests/PlanSets/Projection/AdjustmentRecipeSummaryDtoTests.cs`
- `tests/FloorplanFit.Infrastructure.Tests/PlanSets/CanonicalFloorPlanAdjustmentPersistenceTests.cs`

## Verification
- `rg` confirmed `AdjustmentRecipeJson`, `adjustment_recipe_json`, and recipe derivation references across source and tests.
- `git diff --check` exited 0 with CRLF warnings only.
- Build/test intentionally not run because repo instructions prohibit building after changes.

## Out of scope
Dependent DXF local deformation is still not implemented. Electrical/Roof/Facade currently report local recipe handling and review requirements; actual entity-level stretch replay remains a later slice.
