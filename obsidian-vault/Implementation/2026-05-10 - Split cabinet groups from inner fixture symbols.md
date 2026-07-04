---
type: Implementation
date: 2026-05-10
project: floorplan-fit
status: current
replaces:
  - [[Implementation/2026-05-10 - Relabeled grouped cabinet geometry from nearby fixture text]]
tags:
  - floorplan-fit
  - loop1
  - fixed-components
  - dxf
  - text-hints
  - visual-semantics
---

# Split cabinet groups from inner fixture symbols

## What changed

`IxMiliaFixedPlanComponentExtractor` no longer recolors an entire `CABS-FLOORPLAN` direct-geometry group just because a nearby label says `SINK`, `TUB`, `COOKTOP`, or `OVEN`. It now partitions cabinet-origin grouped geometry into:

- leftover **`Cabinet`** geometry
- inner **`Fixture`** or **`Appliance`** geometry close to the matching text hint

## Why

The first text-hint fix solved semantic detection but created the wrong visual result: some cabinets turned red because the whole cabinet group was reclassified as fixture/appliance. That matched the code, but it did **not** match the intended product behavior. The user wants cabinets to stay cyan while sinks, tubs, and gas/cooktop geometry become curable fixed elements in their own right.

## Current behavior

- Direct `CABS-FLOORPLAN` geometry still groups by proximity first.
- Cabinet-origin groups then look for nearby kitchen/bath text hints.
- Only the **inner seeds** near the hint are emitted as `Fixture` / `Appliance`.
- Remaining geometry from the same grouped area stays `Cabinet`, so cabinet visuals remain cyan.
- This gives Review separate artifacts instead of repainting the whole cabinet area.

## Where

- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaFixedPlanComponentExtractor.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaFixedPlanComponentExtractorTests.cs`
- `obsidian-vault/Current State.md`

## Verification

- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaFixedPlanComponentExtractorTests.ExtractAsync_keeps_cabinet_geometry_separate_when_sink_text_marks_only_inner_symbol" --artifacts-path .\.artifacts-test\cabinet-sink-split` -> 1/1
- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaFixedPlanComponentExtractorTests" --artifacts-path .\.artifacts-test\fixed-extractor-scope-3` -> 6/6
- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaFixedPlanComponentExtractorTests|FullyQualifiedName~OpenFloorPlanReviewSessionIntegrationTests|FullyQualifiedName~FloorPlanReviewSessionReaderIntegrationTests" --artifacts-path .\.artifacts-test\fixed-review-scope-3` -> 8/8

## Gotchas

- The wrong result came from **relabeling the whole group after clustering**, not from the preview renderer.
- This is still heuristic extraction; if future floorplans place labels far from symbols, the hint thresholds may need tuning.
