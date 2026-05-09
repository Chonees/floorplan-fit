---
type: Implementation
date: 2026-05-07
project: floorplan-fit
status: current
tags:
  - floorplan-fit
  - loop1
  - preview
  - refactor
  - curation
---

# Preview layer refactor for CAD-faithful curation

## What changed

The Review preview started moving from one large Avalonia control into focused preview-layer collaborators.

## Why

Loop 1 curation is becoming CAD-faithful: walls, room labels, openings, fixed components, pinches, and future dimensions all need to render and behave predictably. Keeping every concern inside `FloorPlanPreviewControl` would make dimensions and correction tooling fragile.

## Current behavior

- `FloorPlanPreviewControl` remains the Avalonia composition/interaction shell.
- `PreviewArtifactGeometryIndex` owns path-backed artifact classification and hit-test ordering.
- `OpeningPreviewLayerRenderer` owns opening geometry rendering and semantic opening colors.
- `FixedPlanComponentPreviewLayerRenderer` owns fixed component geometry rendering, DXF color preservation, semantic fallback colors, and highlight style.
- `CadTextPreviewLayerRenderer` owns CAD text overlay render plans for room labels and opening labels:
  - insertion-point projection
  - text-height scaling
  - rotation
  - horizontal/vertical alignment
  - baseline-origin behavior
  - forced black preview color for readability
- `PreviewWorkspaceRenderer` owns the dotted workspace background and workspace dot layout.
- `CompressionHandlePreviewLayerRenderer` owns compression handle rendering and the rule that handles disappear while pinch placement is armed.
- `PinchMarkerPreviewLayerRenderer` owns pinch marker rendering, active group/axis styling, and selected-group filtering for runtime compression preview.
- `FloorPlanPreviewControl` now delegates artifact overlay rendering instead of duplicating opening/fixed/text logic inline.

## Where

- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/PreviewArtifactGeometryIndex.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/OpeningPreviewLayerRenderer.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/FixedPlanComponentPreviewLayerRenderer.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/CadTextPreviewLayerRenderer.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/PreviewWorkspaceRenderer.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/CompressionHandlePreviewLayerRenderer.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/PinchMarkerPreviewLayerRenderer.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
- `docs/superpowers/plans/2026-05-07-preview-layer-refactor.md`

## Verification

- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanPreviewControlTests" --artifacts-path .\.artifacts-test\desktop-text-layer-focused` -> 21/21
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-preview-refactor` -> 46/46
- `dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --artifacts-path .\.artifacts-test\application-preview-refactor` -> 23/23
- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --artifacts-path .\.artifacts-test\infrastructure-preview-refactor` -> 34/34
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanPreviewControlTests" --artifacts-path .\.artifacts-test\desktop-preview-shell-layer-focused` -> 25/25
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-preview-shell-refactor` -> 50/50
- `dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --artifacts-path .\.artifacts-test\application-preview-shell-refactor` -> 23/23
- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --artifacts-path .\.artifacts-test\infrastructure-preview-shell-refactor` -> 34/34
- `git diff --check` -> exit 0, with existing LF/CRLF warnings only
- `.artifacts-test` removed after verification

## Gotchas

- This was intentionally a behavior-preserving Desktop refactor; it does not add dimensions yet.
- Text rendering forces black in preview for readability, even when the original DXF label color is white.
- The next refactor slice should prepare the future dimension overlay and/or a higher-level correction backbone; `FloorPlanPreviewControl` is slimmer, but still owns pointer interaction, viewport calculation, collection observation, zoom/pan state, and high-level render composition.
