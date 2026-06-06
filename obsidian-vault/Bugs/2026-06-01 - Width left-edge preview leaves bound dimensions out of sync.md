---
type: Bug
date: 2026-06-01
project: floorplan-fit
status: fixed
tags:
  - floorplan-fit
  - loop1
  - dimensions
  - measurement-bindings
  - reactive-preview
  - width-compression
---

# Width left-edge preview leaves bound dimensions out of sync

## Verified root cause

The Width left-to-right case exposed a geometry-resolution bug in `DimensionIntervalReactiveProjector`.

`FloorPlanPreviewGeometry.CreatePreviewGeometry` compresses the preview by transforming every segment endpoint. When a path has multiple segments and an earlier segment changes length, resolving a saved measurement node by the same global `PositionRatio` on the already-compressed path can land on the wrong physical segment location.

That is why the symptom appeared only in a specific drag direction/case: the cota binding itself was valid, but the live node lookup was re-sampling against a changed total path length instead of preserving the node's original segment + local position.

## Fixed

`DimensionIntervalReactiveProjector.Project` now accepts optional source/original geometry alongside the compressed preview geometry.

Live node resolution now does this:

1. locate the saved node ratio on the source path;
2. capture the source segment `SortOrder` and local segment ratio;
3. resolve that same segment/local ratio on the preview path;
4. fall back to the previous global-ratio behavior if the preview segment cannot be matched.

`FloorPlanPreviewControl` passes the authored `GeometryPaths` as source geometry while using the compressed geometry as preview geometry, so bound cotas follow the same rendered floor plan geometry instead of drifting when path lengths change.

## Why this is the senior model

A measurement node is not just a scalar percentage over whatever path length exists today. It represents a location authored on a specific geometry path. During preview compression, the path can change length segment-by-segment, so the stable identity is: original path + original segment + local position. Global ratio is only a fallback.

## Verification

- RED: `Project_resolves_nodes_by_original_segment_location_when_preview_compression_changes_path_lengths` first failed because the projector API had no `sourceGeometry` input and therefore could not distinguish original segment location from compressed global ratio.
- GREEN: the new regression passed after source/preview geometry resolution was implemented.
- Focused projector suite passed 14/14:
  `dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --filter "FullyQualifiedName~DimensionIntervalReactiveProjectorTests"`
- Application tests passed 85/85:
  `dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj`
- Desktop native preview slice passed 16/16 using a temporary `OutDir` because the desktop app process had the normal output DLLs locked:
  `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~NativeDimensionPreviewControlTests" -p:OutDir="...\artifacts\test-out\desktop\"`
- `git diff --check` exited 0 with only line-ending warnings.
