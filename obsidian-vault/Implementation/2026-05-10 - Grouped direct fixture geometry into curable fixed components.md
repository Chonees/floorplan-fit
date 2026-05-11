---
type: Implementation
date: 2026-05-10
project: floorplan-fit
status: current
tags:
  - floorplan-fit
  - loop1
  - fixed-components
  - dxf
  - grouping
---

# Grouped direct fixture geometry into curable fixed components

## What changed

`IxMiliaFixedPlanComponentExtractor` no longer persists direct `FIXTURES` / `CABS` / `CABS-FLOORPLAN` entities as one review artifact per raw line/arc/polyline. It now clusters nearby direct geometry into grouped fixed components so original-plan tubs, sinks, lavabos, cooktops, ovens, and thick cabinet/fixture outlines become curable as coherent artifacts.

## Why

The DXF seed plans contain real kitchen and bath details as multi-entity direct CAD geometry, not only as named block inserts. When each primitive persisted as its own component, Review showed fragmented red/cyan strokes instead of meaningful curable objects, which made tubs/lavabos/hornallas feel "missing" even though some raw entities were technically extracted.

## Current behavior

- Nested generic inserts are still resolved recursively to recover semantic fixed-component geometry from inner blocks.
- Direct entities on `FIXTURES`, `CABS`, and `CABS-FLOORPLAN` are collected first, then grouped by semantic kind + source layer using a small geometric proximity tolerance.
- Multi-entity direct clusters persist with `SourceEntityKind = COMPONENT-GROUP` and multiple `geometry_paths` under one fixed component row.
- Single direct entities still remain valid curable components when they do not cluster with neighbors.
- `SEMINOLE2000.dxf` now yields grouped direct `FIXTURES` and `CABS-FLOORPLAN` artifacts instead of only atomized strokes.
- `SANTA-BARBARA.dxf` also yields grouped direct `FIXTURES` and `CABS` artifacts alongside named inserts like `STOVE`, `SINK`, `DISHWASHER`, `TUB`, and `TOILET1`.

## Where

- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaFixedPlanComponentExtractor.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaFixedPlanComponentExtractorTests.cs`
- `obsidian-vault/Current State.md`

## Verification

- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaFixedPlanComponentExtractorTests.ExtractAsync_groups_direct_fixture_geometry_into_curable_components" --artifacts-path .\.artifacts-test\direct-group-green` -> 1/1
- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaFixedPlanComponentExtractorTests" --artifacts-path .\.artifacts-test\fixed-extractor-scope-2` -> 4/4
- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~FixedPlanComponent|FullyQualifiedName~OpenFloorPlanReviewSessionIntegrationTests|FullyQualifiedName~FloorPlanReviewSessionReaderIntegrationTests" --artifacts-path .\.artifacts-test\fixed-component-review-scope-2` -> 6/6

## Gotchas

- The missing-objects bug was not mainly unsupported DXF primitives; it was conceptual fragmentation. Supported direct entities were being emitted one by one instead of as a fixture-level artifact.
- Bounding-box proximity is a heuristic, so future Pointe plans may require threshold tuning if unrelated fixture strokes merge.
- This change improves curability first; semantic sub-classification of grouped direct geometry (for example, distinguishing cooktop vs sink without block names) is still a possible future refinement.
