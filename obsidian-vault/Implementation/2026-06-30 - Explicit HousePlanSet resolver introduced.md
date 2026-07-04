# 2026-06-30 - Explicit HousePlanSet resolver introduced

## Type
Implementation

## What
Introduced a durable `HousePlanSet` identity and resolver so HousePlanSet flows no longer have to treat `FloorPlanTemplateId` as the house plan set id.

## Why
The app is being modularized around a house plan package, not isolated floor plans. A real HousePlanSet identity lets future sheets, versions, registrations, projections, exports, and quality data hang from the house package instead of the floor-plan template.

## Where
- `src/FloorplanFit.Domain/PlanSets/HousePlanSet.cs`
- `src/FloorplanFit.Application/Abstractions/IHousePlanSetRepository.cs`
- `src/FloorplanFit.Contracts/PlanSets/ResolveHousePlanSetRequest.cs`
- `src/FloorplanFit.Contracts/PlanSets/ResolveHousePlanSetResponse.cs`
- `src/FloorplanFit.Application/PlanSets/Library/ResolveHousePlanSetHandler.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteHousePlanSetRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs`
- `src/FloorplanFit.Application/PlanSets/Library/GetPlanSetLibraryHandler.cs`

## Boundary
This is a mapping layer over existing floor-plan templates, not a rewrite of floor-plan import/library. `FloorPlanTemplateId` remains the source id; `HousePlanSetId` is now durable and separate when resolved.

## Verification
- RED evidence: tests expected `ResolveHousePlanSetHandler`, `HousePlanSet`, repository, and distinct `HousePlanSetId`; production had no such model/table/resolver.
- GREEN evidence: resolver creates/reuses durable HousePlanSet, Desktop resolves it before PlanSetVersion, and library projection uses existing HousePlanSet identity when available.
- Static check: `git diff --check` passed for touched HousePlanSet files.
- No `dotnet test` / no `dotnet build`, per repo rule.
