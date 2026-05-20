---
type: Implementation
date: 2026-05-07
project: floorplan-fit
status: implemented
replaces:
replaced_by:
---

# Protected detail assemblies for wet-area curation

## What changed

Implemented **protected detail assemblies** as a first-class Loop 1 curation artifact family. These are CAD detail geometries that should remain visible/selectable/protected for future fit, but should not be promoted into structural wall candidates.

## Why

The SEMINOLE2000 master bath/tub area showed a real curation gap: the shower door and labels were detected, but nearby wet-area detail lines were not structural `WALLS`. Promoting weak layers like `MISC`, `HATCH`, or `L1` globally into walls would contaminate wall extraction. The correct senior model is a separate protected detail stream.

## Where

- `DxfExtractionProfile.PointeHomes` now resolves protected detail layers (`MISC`, `HATCH`) as `WetAreaDetail` while keeping `L1`, `WALLS`, and `DIMS` out.
- `IxMiliaProtectedDetailAssemblyExtractor` extracts supported CAD geometry from protected detail layers and groups it by layer/kind.
- `ExtractedProtectedDetailAssembly`, `DetectedProtectedDetailAssembly`, `ProtectedDetailAssemblyDto`, repository interfaces, SQLite repository, and schema tables were added.
- `ExtractWallCandidatesHandler` now orchestrates protected detail extraction/persistence alongside walls, rooms, openings, and fixed components.
- `SqliteFloorPlanReviewSessionReader` returns protected details and their `geometry_paths` in `FloorPlanReviewSessionDto`.
- `FloorPlanReviewViewModel`, `FloorPlanPreviewControl`, `ProtectedDetailPreviewLayerRenderer`, and `ReviewFloorPlanWindow` now show/select/remove protected details.

## UX result

The admin can now see wet-area/detail assemblies in the right panel under **Protected Details**, click their geometry in the preview, highlight them, and remove false positives with **Remove Selected Detail**.

## Fit implication

This does not yet make the Loop 2 solver consume protected details, but the data is now persisted in the curated review stream so future fit can avoid damaging those details when applying pinches.

## Verification

- `dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --artifacts-path .\.artifacts-test\application-protected-detail` -> 24/24
- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --artifacts-path .\.artifacts-test\infrastructure-protected-detail` -> 40/40
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-protected-detail` -> 54/54
- `git diff --check` -> exit 0, line-ending warnings only

## Gotchas

- `L1` remains intentionally excluded from protected detail extraction because SEMINOLE2000 has hundreds of `L1` entities; treating it as protected detail globally would clutter review and likely create false positives.
- `HATCH` is profile-recognized, but the extractor only persists supported geometry entity shapes; native DXF hatch boundary fidelity is still future work.
