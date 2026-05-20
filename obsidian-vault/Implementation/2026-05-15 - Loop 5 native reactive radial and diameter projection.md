---
project: floorplan-fit
repo: https://github.com/Chonees/floorplan-fit
status: active
updated: 2026-05-15
loop: 5
area: dimensions-radial-diameter
---

# 2026-05-15 - Loop 5 native reactive radial and diameter projection

## What
Started Loop 5 with the first real reactive slice for `Radius` and `Diameter`. These dimension kinds now resolve to typed semantic bindings with `Radial` measured spans, and the reactive preview/export path can rebuild them from live anchors instead of leaving them authored/static.

## Why
After linear + ordinate support, the next correctness gap was non-linear radial dimensions. Keeping them static would break the promise that affected native dimensions follow floor-plan movement, but degrading them to linears would be semantically wrong. The right move was a native rebuild path that preserves the authored CAD family while updating the measured value.

## Core behavior
- `DimensionBindingProjector` now resolves:
  - `Radius`
  - `Diameter`
- Reactive rebuild now uses a **similarity transform** from old anchors to new anchors:
  - translate
  - rotate
  - scale
- This transform is applied to authored primitives:
  - line primitives
  - text primitives
  - insert primitives
  - circle primitives
  - arc primitives
  - solid primitives
- Measured value is recomputed from the live anchor distance.

## Files
- `src/FloorplanFit.Application/FloorPlans/Review/DimensionBindingProjector.cs`
- `src/FloorplanFit.Application/FloorPlans/Review/ReactiveDimensionProjector.cs`
- `src/FloorplanFit.Application/FloorPlans/Review/DimensionGeometryProjector.cs`
- `tests/FloorplanFit.Application.Tests/FloorPlans/Review/DimensionBindingProjectorTests.cs`
- `tests/FloorplanFit.Application.Tests/FloorPlans/Review/ReactiveDimensionProjectorTests.cs`
- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/ExportAdjustedDxfHandlerTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/NativeDimensionPreviewControlTests.cs`

## Verification
- `dotnet test .\\tests\\FloorplanFit.Application.Tests\\FloorplanFit.Application.Tests.csproj --filter "DimensionBindingProjectorTests|ReactiveDimensionProjectorTests|ExportAdjustedDxfHandlerTests"`
- `dotnet test .\\tests\\FloorplanFit.Desktop.Tests\\FloorplanFit.Desktop.Tests.csproj --filter "NativeDimensionPreviewControlTests|PreviewCollectionObserverHubTests|FloorPlanReviewViewModelTests|DimensionEditingFloorPlanReviewViewModelTests" --artifacts-path .\\.artifacts-test\\desktop-loop5-radial-wide`

## Notes
- The similarity-transform approach fits this repo better than a fake linear rebuild because it preserves authored orientation and scale of radial callouts.
- This slice does not claim that product-facing manual edit semantics for radial/diameter are fully finalized yet; it closes the reactive preview/export side first.
