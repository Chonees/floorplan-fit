---
type: Implementation
date: 2026-05-07
project: floorplan-fit
status: current
tags:
  - floorplan-fit
  - loop1
  - fixed-components
  - dxf
  - review-ui
---

# Fixed plan components extracted into review

## What changed

Loop 1 review now extracts fixed plan components from the DXF and treats them as their own curation stream, separate from walls and openings.

## Why

Toilets, fixtures, appliances, cabinets, and inserted CAD blocks are real plan objects. They must be visible and removable during curation because the future site-plan fit should know which details are protected and which false positives should not persist.

## Current behavior

- `IxMiliaFixedPlanComponentExtractor` reads fixed/protected plan geometry from:
  - `FIXTURES`
  - `CABS`
  - `CABS-FLOORPLAN`
  - relevant `INSERT` blocks such as `TOILET1`, `STOVE`, `SINK`, `DISHWASHER`, `TUB`, and `WASH_DRY`
- Since 2026-05-10, the extractor also walks **nested/generic block inserts** recursively so cabinet or fixture assemblies do not drop real original-plan sub-blocks just because the outer insert sits on layer `0` / `2` or uses a non-semantic block name.
- Extracted components persist as `ExtractedFixedPlanComponent`.
- Each component can own one or more persisted `geometry_paths`.
- The Review session exposes `FixedPlanComponentDto`.
- The preview renders fixed components as a separate overlay and prioritizes them in hit-testing above openings and walls.
- Fixed components preserve their original DXF color when available (`ColorArgb`) instead of always using semantic fallback colors.
- Clicking a fixed component on the preview selects it.
- The right panel has a `Fixed Elements` section and a `Remove Selected Component` action for false positives.

## Where

- `src/FloorplanFit.Application/Abstractions/DetectedFixedPlanComponent.cs`
- `src/FloorplanFit.Application/Abstractions/IFixedPlanComponentExtractor.cs`
- `src/FloorplanFit.Application/Abstractions/IExtractedFixedPlanComponentRepository.cs`
- `src/FloorplanFit.Application/FloorPlans/Curation/RemoveFixedPlanComponentHandler.cs`
- `src/FloorplanFit.Contracts/FloorPlans/FixedPlanComponentDto.cs`
- `src/FloorplanFit.Domain/FloorPlans/ExtractedFixedPlanComponent.cs`
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaFixedPlanComponentExtractor.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedFixedPlanComponentRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`

## Verification

- `dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --artifacts-path .\.artifacts-test\application-full` -> 23/23
- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --artifacts-path .\.artifacts-test\infrastructure-full` -> 34/34
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-full-2` -> 42/42
- `dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --artifacts-path .\.artifacts-test\application-full-color` -> 23/23
- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --artifacts-path .\.artifacts-test\infrastructure-full-color-2` -> 34/34
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-full-color` -> 43/43
- `git diff --check` -> exit 0

## Gotchas

- DXF block inserts need transformation from block-local coordinates into model coordinates using insertion point, scale, rotation, and block base point.
- Color resolution needs CAD semantics: `BYLAYER` resolves through the entity layer; nested block entities on layer `0` inherit the insert layer; `BYBLOCK` falls back to the insert color when available.
- Generic wrapper inserts are not safe rejection criteria. The semantic signal may live deeper in nested block entities or their layers, so extraction now resolves kind/source layer/color recursively before giving up.
- Components are not openings. They should not feed door/window protection logic directly; they are fixed/protected plan details for curation and future fit constraints.
- Because `geometry_segments` is still linear-only, arcs/circles/ellipses from fixture geometry are flattened into segment paths for preview/persistence.
