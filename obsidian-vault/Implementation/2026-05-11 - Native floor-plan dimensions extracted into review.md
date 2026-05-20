# Native floor-plan dimensions extracted into review

## What changed
- Added `IxMiliaDimensionExtractor` to read native DXF `DIMENSION` entities from floor plans.
- Excluded the `ELECTRICAL WIRING` layer from this first Loop 1 slice.
- Reconstructed measurement values when IxMilia leaves `ActualMeasurement` at zero, while still preserving the CAD-visible `DisplayText`.
- Added persistence for extracted dimensions in SQLite via `extracted_dimensions`.
- Exposed dimensions in `FloorPlanReviewSessionDto` and surfaced them in the Review Queue as a read-only `Dimensions` section.

## Verified scope
- `SEMINOLE2000.dxf`: 326 in-scope native dimensions
- `SANTA-BARBARA.dxf`: 114 native dimensions
- Display text priority remains: geometry block text -> DXF override text -> generated fallback

## Key files
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaDimensionExtractor.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedDimensionRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaDimensionExtractorTests.cs`

## Why it matters
This gives Loop 1 review a CAD-faithful dimensions family instead of forcing users to infer measurements from preview geometry alone.
