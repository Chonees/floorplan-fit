---
project: floorplan-fit
repo: https://github.com/Chonees/floorplan-fit
status: active
updated: 2026-05-15
loop: 6
area: dimensions-topology-hardening
---

# 2026-05-15 - Loop 6 topology-aware anchor remap for reactive dimensions

## What
Started Loop 6 with the first hardening slice: reactive dimension projection no longer trusts `EdgeKey` as an absolute identity. It now performs a topology-aware remap for anchors when the live geometry has split or reordered segments.

## Why
The previous associative loops assumed a stable one-to-one mapping between stored `EdgeKey` and live measurable edge. That was too brittle. In real preview topology changes, a projected anchor can fail in two ways:

1. the old `EdgeKey` disappears and the dimension goes static
2. the old `EdgeKey` still exists, but now points to the wrong split segment

The second bug is nastier because it looks “reactive” while actually measuring the wrong point.

## Core behavior
- For each anchor, the projector now:
  - tries the direct `EdgeKey`
  - also evaluates live candidate edges on the same `GeometryPathId` / artifact
  - chooses the live point that best matches the **stored anchor coordinate**
- For projected anchors, remap uses the stored anchor coordinate projected onto each live segment.
- If the direct edge still exists but is now worse than another split segment, the projector switches to the better one.

## Files
- `src/FloorplanFit.Application/FloorPlans/Review/ReactiveDimensionProjector.cs`
- `tests/FloorplanFit.Application.Tests/FloorPlans/Review/ReactiveDimensionProjectorTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/NativeDimensionPreviewControlTests.cs`

## Verification
- `dotnet test .\\tests\\FloorplanFit.Application.Tests\\FloorplanFit.Application.Tests.csproj --filter "DimensionBindingProjectorTests|ReactiveDimensionProjectorTests|ExportAdjustedDxfHandlerTests"`
- `dotnet test .\\tests\\FloorplanFit.Desktop.Tests\\FloorplanFit.Desktop.Tests.csproj --filter "NativeDimensionPreviewControlTests|PreviewCollectionObserverHubTests|FloorPlanReviewViewModelTests|DimensionEditingFloorPlanReviewViewModelTests" --artifacts-path .\\.artifacts-test\\desktop-loop6-wide`

## Notes
- This is the first real hardening slice, not the full Loop 6 closure.
- It covers split/reorder drift where path/artifact identity survives.
- More aggressive topology mutations still need future handling.
