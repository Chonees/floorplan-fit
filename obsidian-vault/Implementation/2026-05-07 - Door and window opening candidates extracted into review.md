---
project: floorplan-fit
type: implementation
date: 2026-05-07
status: implemented
replaces: []
replaced_by:
---

# Door and window opening candidates extracted into review

## What

Loop 1 now extracts doors and windows from the DXF as opening candidates, persists them by extraction run, loads them into the review session, and renders their geometry plus exact model/size labels on the preview canvas.

## Why

The user needs the curation/fit workflow to know where openings live so future site-plan adaptation can avoid cutting through doors/windows and can preserve labels such as `2668`, `3050 S.H.`, and `(3) 3050 FXD. HDR. @ 6'-8"`.

## Source of truth

- Door geometry: DXF layer `DOORS`
- Window geometry: DXF layers `WIN` and `WINS`
- Door labels: DXF layer `DOORTEXT`
- Window labels: DXF layer `WINDWS LBLS`

## Where

- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaOpeningExtractor.cs` extracts opening geometry and labels from IxMilia DXF entities.
- `src/FloorplanFit.Application/Abstractions/IOpeningExtractor.cs` adds the application boundary.
- `src/FloorplanFit.Domain/FloorPlans/ExtractedOpeningCandidate.cs` and `ExtractedOpeningLabel.cs` model the persisted extraction result.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedOpeningCandidateRepository.cs` persists opening geometry paths.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedOpeningLabelRepository.cs` persists exact text labels and DXF text metadata.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs` loads openings into review sessions.
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs` renders opening geometry and labels and excludes opening paths from wall hit-testing.
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml` shows the `Openings` side-panel and binds openings into the preview.

## Notes / gotchas

- Existing extraction runs will not be backfilled automatically. Re-run `Extract Walls` for a template if the Review does not show openings.
- The current runtime geometry schema stores line segments only. Door arcs from DXF are flattened into short line segments for visual preview. This is good for MVP rendering, but exact native arc persistence would require extending `geometry_segments` with arc fields.
- Labels are not normalized into semantic sizes yet; they are preserved as exact visible text from DXF label layers.

## Verification

- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaOpeningExtractorTests"` -> 3/3 passed.
- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj` -> 30/30 passed.
- `dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj` -> 20/20 passed.
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj` -> 35/35 passed.
- `git diff --check` -> exit 0.
