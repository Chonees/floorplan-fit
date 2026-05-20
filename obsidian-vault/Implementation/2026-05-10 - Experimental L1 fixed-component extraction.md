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
  - l1
  - experiment
---

# Experimental L1 fixed-component extraction

## What changed

`DxfExtractionProfile.PointeHomes` now admits layer `L1` as `Fixture` for fixed-component extraction.

## Why

Direct AutoCAD inspection of a missing sink showed that the symbol lives entirely on layer `L1` and is made of supported entities (`3DFACE`, `ELLIPSE`, `CIRCLE`, `ARC`). The extractor was not failing on geometry support; it was filtering the symbol out before extraction because `L1` was not treated as a fixed-component layer.

## Current behavior

- Direct geometry on `L1` now enters the fixed-component extractor.
- Supported `L1` geometry gets grouped and persisted like other fixed components.
- This is intentionally a broad experiment to surface more missing kitchen/bath symbols in Review before we tighten recognition rules.
- Because `L1` is broad, this change may introduce extra noise in `Fixed Elements`; that is an accepted short-term tradeoff for this diagnostic pass.

## Where

- `src/FloorplanFit.Infrastructure/Dxf/DxfExtractionProfile.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaFixedPlanComponentExtractorTests.cs`
- `obsidian-vault/Bugs/2026-05-10 - Missing sink symbols live on L1 so fixed-component extractor skips them.md`

## Verification

- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaFixedPlanComponentExtractorTests.ExtractAsync_includes_supported_l1_fixture_geometry_when_layer_is_opened_for_review" --artifacts-path .\.artifacts-test\l1-green` -> 1/1
- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaFixedPlanComponentExtractorTests|FullyQualifiedName~OpenFloorPlanReviewSessionIntegrationTests|FullyQualifiedName~FloorPlanReviewSessionReaderIntegrationTests" --artifacts-path .\.artifacts-test\l1-scope` -> 9/9
- Runtime verification after relaunch + clean reimport/reextract: latest `app.db` run at `2026-05-11T02:00:48Z` persisted `28` fixed components on `source_layer = L1`, proving the preview only started showing them once the updated binary actually drove a fresh extraction.

## Gotchas

- This does not magically reconstruct perfect sinks/tubs/cooktops; it only stops filtering `L1` out at the profile gate.
- To see the effect in the app, a fresh extraction run is required; old review sessions keep old persisted artifacts.
- Verified runtime gotcha on 2026-05-10: the source file enabling `L1` changed at 22:06 local time, but the Desktop debug DLLs on disk were still last built at 21:37-21:39. A later extraction run at 22:56 still produced `l1_component_count = 0` in `app.db`, which proves that running an old binary will never surface the new `L1` artifacts no matter how many times Review is reopened.
- The successful user flow was stronger than a simple reopen: restarting onto the updated binary, wiping the old library state, reimporting, and extracting again guaranteed a brand-new persisted extraction run instead of re-reading stale artifact rows.
