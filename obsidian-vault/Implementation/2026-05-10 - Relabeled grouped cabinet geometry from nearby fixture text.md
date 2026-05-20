---
type: Implementation
date: 2026-05-10
project: floorplan-fit
status: superseded
replaced_by:
  - [[Implementation/2026-05-10 - Split cabinet groups from inner fixture symbols]]
tags:
  - floorplan-fit
  - loop1
  - fixed-components
  - dxf
  - text-hints
---

# Relabeled grouped cabinet geometry from nearby fixture text

> Superseded on 2026-05-10 by [[Implementation/2026-05-10 - Split cabinet groups from inner fixture symbols]] because relabeling the whole grouped component changed cabinet visuals instead of isolating the inner fixture/appliance geometry.

## What changed

`IxMiliaFixedPlanComponentExtractor` now scans top-level DXF `TEXT` / `MTEXT` for kitchen and bath labels (`SINK`, `DISP`, `COOKTOP`, `OVEN`, `TUB`, `SHWR`, `LAV`, etc.) and uses the nearest matching label to relabel grouped direct fixed-component geometry.

## Why

After grouping direct `CABS-FLOORPLAN` geometry, the extractor still classified many real fixtures and appliances as `Cabinet` because layer-only semantics were too weak. In `SEMINOLE2000.dxf`, objects near `SINK & DISP.`, `36" COOKTOP`, `OVEN`, and `TUB & SHWR.` were present in extraction but hidden semantically inside cabinet groups, so the user still felt those plan objects were "missing" in Review.

## Current behavior

- Grouped direct geometry still starts from layer-based classification.
- If a nearby CAD text hint matches appliance semantics, the grouped component is relabeled to `Appliance`.
- If a nearby CAD text hint matches sink/tub/shower/lav semantics, the grouped component is relabeled to `Fixture`.
- The original grouping behavior stays intact; this change only corrects the semantic kind that Review receives.
- Detection notes now record the nearby text hint used for the semantic override.

## Where

- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaFixedPlanComponentExtractor.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaFixedPlanComponentExtractorTests.cs`
- `obsidian-vault/Current State.md`

## Verification

- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaFixedPlanComponentExtractorTests.ExtractAsync_relabels_seminole_cabinet_layer_groups_from_nearby_fixture_text" --artifacts-path .\.artifacts-test\fixture-text-green` -> 1/1
- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaFixedPlanComponentExtractorTests|FullyQualifiedName~OpenFloorPlanReviewSessionIntegrationTests|FullyQualifiedName~FloorPlanReviewSessionReaderIntegrationTests" --artifacts-path .\.artifacts-test\fixture-text-scope` -> 7/7

## Gotchas

- The missing-preview symptom was not a renderer bug; the objects were already persisted but semantically mislabeled as `Cabinet`.
- Text-hint relabeling is heuristic and proximity-based, so future Pointe plans may need token/threshold tuning if labels move far away from their symbols.
