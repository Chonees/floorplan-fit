# 2026-06-30 - Explicit PlanSetVersion resolver introduced

## Type
Implementation

## What
Introduced an explicit durable `PlanSetVersion` model and resolver so HousePlanSet flows no longer need to treat `FloorPlanVersionId` as the plan-set version id.

## Why
The HousePlanSet architecture needs a real version boundary for a package of sheets. The floor plan remains the canonical source sheet, but the package version must be its own identity so dependent sheets, registrations, projections, canonical adjustments, exports, and quality events can all attach to the same plan-set version.

## Where
- `src/FloorplanFit.Domain/PlanSets/PlanSetVersion.cs`
- `src/FloorplanFit.Application/Abstractions/IPlanSetVersionRepository.cs`
- `src/FloorplanFit.Contracts/PlanSets/ResolvePlanSetVersionRequest.cs`
- `src/FloorplanFit.Contracts/PlanSets/ResolvePlanSetVersionResponse.cs`
- `src/FloorplanFit.Application/PlanSets/Library/ResolvePlanSetVersionHandler.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqlitePlanSetVersionRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs`
- `src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs`
- `src/FloorplanFit.Application/PlanSets/Library/GetPlanSetLibraryHandler.cs`

## Boundary
This is the smallest durable version boundary. It does not create a separate HousePlanSet aggregate table yet; `HousePlanSetId` still maps to the existing floor plan template id. The old `FloorPlanVersionId == PlanSetVersionId` bridge remains only as fallback when no explicit plan-set version exists.

## Verification
- RED evidence: tests expected `ResolvePlanSetVersionHandler`, `PlanSetVersion`, repository, and distinct `PlanSetVersionId`; production had no such files/table.
- GREEN evidence: resolver creates/reuses a durable `PlanSetVersion`; Desktop resolves it before Adjust-to-Site-Plan; package/canonical adjustment receives the explicit id; library projection reads dependent sheets by explicit plan-set id when available.
- Static check: `git diff --check` passed for touched files.
- No `dotnet test` / no `dotnet build`, per repo rule.
