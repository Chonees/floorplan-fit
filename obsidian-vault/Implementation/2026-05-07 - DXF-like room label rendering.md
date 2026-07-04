---
type: Implementation
project: floorplan-fit
date: 2026-05-07
status: current
replaces: [[Implementation/2026-05-07 - Room label overlay rendered on preview canvas]]
replaced_by:
---

# DXF-like room label rendering

## What

Room labels now render on the Review canvas as raw DXF-like text instead of artificial yellow badges.

## Why

The badge overlay proved visually imprecise: it used a fixed font size, artificial offsets, a yellow rounded rectangle and no DXF text metadata. That could never match the original DXF.

## Where

- `src/FloorplanFit.Application/Abstractions/DetectedRoomLabel.cs`
- `src/FloorplanFit.Contracts/FloorPlans/RoomLabelDto.cs`
- `src/FloorplanFit.Domain/FloorPlans/ExtractedRoomLabel.cs`
- `src/FloorplanFit.Application/FloorPlans/Extraction/ExtractWallCandidatesHandler.cs`
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaRoomLabelExtractor.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedRoomLabelRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`

## Behavior

- The extractor preserves DXF room label visual metadata: entity kind, text height, rotation, style name, horizontal/vertical alignment, attachment point and color.
- SQLite persists the new metadata and migrates existing local tables with nullable columns.
- The Review reader exposes the metadata through `RoomLabelDto`.
- The preview canvas renders raw text at `RoomLabelDto.X/Y`, scales font size from `TextHeight * viewport.Scale`, applies rotation, and removes the badge/anchor-dot overlay.

## Current limitation

This is DXF-like rendering, not a full CAD text engine. If a DXF uses SHX fonts unavailable to Avalonia/Windows font rendering, exact glyph shapes can still differ. The next level of fidelity would require SHX/font-file resolution or rasterizing text from a CAD engine.

## Verification

- RED tests failed first because room labels did not expose visual metadata and the preview did not create a render plan from DXF height/rotation.
- Focused tests passed after implementation.
- Full sequential verification:
  - `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj` -> 20/20
  - `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj` -> 26/26
  - `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj` -> 24/24
  - `git diff --check` -> exit 0

## Gotcha

`dotnet test --no-restore` can leave Avalonia generated XAML artifacts stale. The full Desktop suite passed with normal `dotnet test`, and adding manual duplicate `AvaloniaResource` entries is wrong because Avalonia already includes `.axaml` by default.

## Links

- [[Current State]]
- [[Implementation/2026-05-07 - Room label candidates extracted into review]]
